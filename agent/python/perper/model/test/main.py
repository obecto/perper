import asyncio

from pyignite.datatypes.primitive_objects import BoolObject
from pyignite import Client

from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService
from perper.protocol.standard import PerperStream
from perper.model.stream import Stream
from perper.model.async_locals import *
from perper.model.context import *

ignite = Client()
async def test():
    with ignite.connect('127.0.0.1', 10800):
        cache_service = CacheService(ignite)
        notification_service = NotificationService(ignite, '127.0.0.1:40400', 'test_agent1')
        set_connection(cache_service, notification_service)

        agent = await enter_context('test_agent1', lambda: start_agent('test_agent2', True, BoolObject))
        print(agent)
        
        # notification_service.start()
        # cache_service.stream_create('test_stream', 'test_instance', 'test_agent', 'test_delegate', 2, True, BoolObject)
        # stream = Stream(PerperStream('test_stream', {}, False, False))

        # async def listen_items():
        #     generator = enter_context('test_instance',  lambda: stream.enumerate())
        #     async for value in generator:
        #         print(value)

        # await listen_items()

asyncio.run(test())