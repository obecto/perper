import os
import sys
import asyncio
import random
from pyignite import Client
from pyignite.datatypes.primitive_objects import BoolObject
from perper.model.async_locals import *
from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService
from perper.model.context import *

async def initialize(agent, functions, root=False):
    ignite = Client()
    ignite_address = os.getenv('PERPER_IGNITE_ADDRESS', '127.0.0.1')
    ignite_port = int(os.getenv('PERPER_IGNITE_PORT', '10800'))
    ignite.connect(ignite_address, ignite_port)

    cache_service = CacheService(ignite)
    grpc_address = os.getenv('PERPER_GRPC_ADDRESS', '127.0.0.1:40400')
    notification_service = NotificationService(ignite, grpc_address, agent)
    await notification_service.start()
    set_connection(cache_service, notification_service)

    task1 = asyncio.create_task(listen_triggers(ignite, functions))
    task2 = asyncio.create_task(enter_context('', lambda: start_agent(agent, None, BoolObject)))

    if root:
        await task1
        await task2
    else:
        await task1

async def execute_call(functions, call_data, n):
    (result, result_type) = await enter_context(call_data.instance, lambda: asyncio.create_task(functions[n.delegate](call_data.parameters)))
    get_cache_service().call_write_result(n.call, result, result_type)

async def listen_triggers(ignite, functions):
    async for (k, n) in get_notification_service().get_notifications(get_local_agent()):
        get_notification_service().consume_notification(k)

        incoming_type = n.__class__.__name__
        if incoming_type == 'StreamTriggerNotification':
            streams_cache = ignite.get_cache('streams')
            stream_data = streams_cache.get(n.stream)
            stream_cache = ignite.get_cache(n.stream)

            if n.delegate in functions:
                generator = functions[n.delegate](stream_data.parameters)
                async for data in generator:
                    stream_cache.put(random.randrange(1, sys.maxsize), data)

        if incoming_type == 'CallTriggerNotification':
            calls_cache = ignite.get_cache('calls')
            call_data = calls_cache.get(n.call)

            if n.delegate in functions:
                asyncio.create_task(execute_call(functions, call_data, n))
