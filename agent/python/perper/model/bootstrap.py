import os
import sys
import asyncio
import random
import traceback
from pyignite import Client
from pyignite.datatypes.primitive_objects import BoolObject
from perper.model.async_locals import *
from perper.model.task_collection import TaskCollection
from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService
from perper.model.context import *

def initialize_connection(agent):
    ignite = Client()
    (ignite_address, ignite_port) = os.getenv('APACHE_IGNITE_ENDPOINT', '127.0.0.1:10800').split(':')
    ignite_port = int(ignite_port)
    ignite.connect(ignite_address, ignite_port)

    cache_service = CacheService(ignite)
    grpc_endpoint = os.getenv('PERPER_FABRIC_ENDPOINT', '127.0.0.1:40400')
    notification_service = NotificationService(ignite, grpc_endpoint, agent)

    set_connection(cache_service, notification_service) # It is important that this call occurs in a sync context

    return asyncio.create_task(notification_service.start())

async def initialize(agent, functions, root=False):
    await initialize_connection(agent)

    task_collection = TaskCollection()
    task_collection.add(listen_triggers(task_collection, functions))
    if root:
        task_collection.add(enter_context('', lambda: start_agent(agent, [])))

    try:
        await task_collection
    finally:
        await get_notification_service().stop()

def initialize_notebook(functions = {}, agent = None):
    if agent is None: agent = CacheService.generate_name('notebook')

    task_collection = TaskCollection()

    task_collection.add(initialize_connection(agent))
    set_context(get_cache_service().generate_name(agent))

    task_collection.add(listen_triggers(task_collection, functions))

    return task_collection

async def listen_triggers(task_collection, functions):
    async def process_stream(generator, stream):
        async for data in generator:
            get_cache_service().stream_write_item(stream, data)
    async def process_notification(k, n):
        get_notification_service().consume_notification(k)

        incoming_type = n.__class__.__name__
        if incoming_type == 'StreamTriggerNotification':
            instance = get_cache_service().stream_get_instance(n.stream)
            parameters = get_cache_service().stream_get_parameters(n.stream)

            if n.delegate in functions:
                try:
                    generator = await enter_context(instance, lambda: asyncio.create_task(process_stream(functions[n.delegate](*parameters), n.stream)))
                except Exception as ex:
                    print("Error while invoking", n.delegate, ":")
                    traceback.print_exception(type(ex), ex, ex.__traceback__)

        if incoming_type == 'CallTriggerNotification':
            instance = get_cache_service().call_get_instance(n.call)
            parameters = get_cache_service().call_get_parameters(n.call)

            if n.delegate in functions:
                try:
                    return_value = await enter_context(instance, lambda: asyncio.create_task(functions[n.delegate](*parameters)))
                    if return_value == None:
                        get_cache_service().call_write_finished(n.call)
                    else:
                        (result, result_type) = return_value
                        get_cache_service().call_write_result(n.call, result, result_type)
                except Exception as ex:
                    print("Error while invoking", n.delegate, ":")
                    traceback.print_exception(type(ex), ex, ex.__traceback__)

    async for (k, n) in get_notification_service().get_notifications(get_local_agent()):
        task_collection.add(process_notification(k, n))

        get_notification_service().consume_notification(k)


