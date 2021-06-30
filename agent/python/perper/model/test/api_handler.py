import sys
import asyncio
import random
from perper.model.async_locals import *

async def listen_triggers(ignite, functions):
    async for (k, n) in get_notification_service().get_notifications(get_local_agent()):
        print(n)
        get_notification_service().consume_notification(k)

        incoming_type = n.__class__.__name__
        if incoming_type == 'StreamTriggerNotification':
            streams_cache = ignite.get_cache('streams')
            stream_data = streams_cache.get(n.stream)
            stream_cache = ignite.get_cache(n.stream)

            if n.delegate in functions:
                async for data in functions[n.delegate](stream_data.parameters):
                    print('putting')
                    stream_cache.put(random.randrange(1, sys.maxsize), data)

        if incoming_type == 'CallTriggerNotification':
            calls_cache = ignite.get_cache('calls')
            call_data = calls_cache.get(n.call)

            if n.delegate in functions:
                (result, result_type) = await asyncio.create_task(functions[n.delegate](call_data.parameters))
                get_cache_service().call_write_result(n.call, result, result_type)
