import sys
import asyncio
import random
from pyignite import Client
from perper.model.async_locals import *
from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService

async def initialize(agent, functions, root=False):
    ignite = Client()
    ignite.connect('127.0.0.1', 10800)

    cache_service = CacheService(ignite)
    notification_service = NotificationService(ignite, '127.0.0.1:40400', agent)
    notification_service.start()
    set_connection(cache_service, notification_service)

    if root:
        await enter_context(f'{agent}-root', functions[agent])
    else:
        await listen_triggers(ignite, functions)


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
                (result, result_type) = await enter_context(call_data.instance, lambda: asyncio.create_task(functions[n.delegate](call_data.parameters)))
                get_cache_service().call_write_result(n.call, result, result_type)
