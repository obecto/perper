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

def initialize_connection(agent, use_instance=False):
    instance = os.getenv('X_PERPER_INSTANCE') if use_instance else None

    ignite = Client()
    (ignite_address, ignite_port) = os.getenv('APACHE_IGNITE_ENDPOINT', '127.0.0.1:10800').split(':')
    ignite_port = int(ignite_port)
    ignite.connect(ignite_address, ignite_port)

    cache_service = CacheService(ignite)
    grpc_endpoint = os.getenv('PERPER_FABRIC_ENDPOINT', '127.0.0.1:40400')
    notification_service = NotificationService(ignite, grpc_endpoint, agent, instance)

    set_connection(cache_service, notification_service) # It is important that this call occurs in a sync context

    return asyncio.create_task(notification_service.start())

async def initialize(agent, calls = {}, streams = {}):
    await initialize_connection(agent)

    task_collection = TaskCollection()

    if 'Init' in calls:
        init_function = calls.pop('Init')
        async def invoke_init():
            try:
                await enter_context(agent + '-Init', 'Init-Init', init_function)
            except Exception as ex:
                print("Error while invoking Init:")
                traceback.print_exception(type(ex), ex, ex.__traceback__)
        task_collection.add(invoke_init())

    for (delegate, function) in calls.items():
        task_collection.add(listen_call_triggers(delegate, task_collection, function))
    for (delegate, function) in streams.items():
        task_collection.add(listen_stream_triggers(delegate, task_collection, function))

    try:
        await task_collection
    finally:
        await get_notification_service().stop()

def initialize_notebook(agent = None):
    if agent is None: agent = CacheService.generate_name('notebook')

    task_collection = TaskCollection()

    task_collection.add(initialize_connection(agent))
    set_context(agent + '-Init', 'Init-Init')

    return task_collection

async def listen_call_triggers(delegate, task_collection, function):
    async def process_notification(k, n):
        parameters = get_cache_service().call_get_parameters(n.call)

        try:
            return_value = await enter_context(n.instance, n.call, lambda: asyncio.create_task(function(*parameters)))
            if return_value == None:
                get_cache_service().call_write_finished(n.call)
            else:
                (result, result_type) = return_value
                get_cache_service().call_write_result(n.call, result, result_type)
        except Exception as ex:
            print("Error while invoking", delegate, ":")
            traceback.print_exception(type(ex), ex, ex.__traceback__)

    async for (k, n) in get_notification_service().get_notifications(NotificationService.CALL, delegate):
        task_collection.add(process_notification(k, n))

        get_notification_service().consume_notification(k)

async def listen_stream_triggers(delegate, task_collection, functions):
    async def process_stream(generator, stream):
        async for data in generator:
            get_cache_service().stream_write_item(stream, data)

    async def process_notification(k, n):
        parameters = get_cache_service().stream_get_parameters(n.stream)

        try:
            generator = await enter_context(n.instance, n.stream, lambda: asyncio.create_task(process_stream(function(*parameters), n.stream)))
        except Exception as ex:
            print("Error while invoking", delegate, ":")
            traceback.print_exception(type(ex), ex, ex.__traceback__)


    async for (k, n) in get_notification_service().get_notifications(NotificationService.STREAM, delegate):
        task_collection.add(process_notification(k, n))

        get_notification_service().consume_notification(k)
