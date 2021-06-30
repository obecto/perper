import asyncio
from pyignite import Client
from pyignite.datatypes.standard import String
from pyignite.datatypes.primitive_objects import BoolObject

from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService
from perper.model.async_locals import *
from perper.model.context import *
from perper.model.agent import Agent
from perper.model.stream import Stream

ignite = Client()
ignite.connect('127.0.0.1', 10800)

async def test():
    cache_service = CacheService(ignite)
    notification_service = NotificationService(ignite, '127.0.0.1:40400', 'test_agent1')
    notification_service.start()
    set_connection(cache_service, notification_service)

    (agent2, result) = await enter_context('test_agent1', lambda: start_agent('test_agent2', True, BoolObject))
    print(agent2)
    
    perper_agent3 = await agent2.call_function('get_next_agent', True, BoolObject)
    agent3 = Agent(perper_agent3)
    print(agent3)
    
    inc_stream = await agent3.call_function('get_stream', True, BoolObject)
    async for v in Stream(inc_stream).enumerate():
        print(v)
        if v == 9:
            return

asyncio.create_task(test())