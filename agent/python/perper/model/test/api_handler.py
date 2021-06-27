from pyignite import Client
import asyncio
from pyignite.datatypes.primitive_objects import BoolObject
from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService

ignite = Client()
with ignite.connect('127.0.0.1', 10800):
    cache_service = CacheService(ignite)
    notification_service = NotificationService(ignite, '127.0.0.1:40400', 'test_agent1')
    notification_service.start()

    async def listen_triggers(functions):
        print('to be or not to be')
        async for (k, n) in notification_service.get_notifications('test_agent2'):
            print(n)
            incoming_type = n.__class__.__name__
            if incoming_type == 'CallTriggerNotification':
                notification_service.consume_notification(k)
                calls_cache = ignite.get_cache("calls")
                call_data = calls_cache.get(n.call)

                # if n.delegate in functions:
                #     result = await asyncio.create_task(functions[n.delegate](call_data.parameters))
                #     call_data.result = result
                #     call_data.finished = True
                #     calls_cache.replace(n.call, call_data)
    
    asyncio.run(listen_triggers({}))