import sys
import asyncio
import random
from pyignite import Client
from pyignite.datatypes.primitive_objects import BoolObject
from pyignite.datatypes.standard import String
from pyignite.datatypes.complex import BinaryObject
from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService
from perper.model.async_locals import enter_context
from perper.model.context import *

ignite = Client()
with ignite.connect('127.0.0.1', 10800):
    cache_service = CacheService(ignite)
    notification_service = NotificationService(ignite, '127.0.0.1:40400', 'test_agent2')
    set_connection(cache_service, notification_service)
    notification_service.start()

    async def test(params):
        return ('False123', String)

    async def get_stream(params):
        stream = enter_context('test_agent2', lambda: stream_function('generate', True, BoolObject))
        return (stream.raw_stream, BinaryObject)

    async def generate(params):
        for x in range(10):
            await asyncio.sleep(1)
            yield x

    async def listen_triggers(functions):
        async for (k, n) in notification_service.get_notifications('test_agent2'):
            print(n)
            notification_service.consume_notification(k)

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
                    cache_service.call_write_result(n.call, result, result_type)
    
    asyncio.run(listen_triggers({'test_agent2': test, 'get_stream': get_stream, 'generate': generate}))