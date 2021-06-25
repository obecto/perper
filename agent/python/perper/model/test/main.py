import asyncio

from pyignite.datatypes.primitive_objects import BoolObject
from perper.protocol.thin_client import PerperIgniteClient

from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService
from perper.protocol.standard import PerperStream
from perper.model.stream import Stream
from perper.model.async_locals import *

ignite = PerperIgniteClient()
def test():
    with ignite.connect('127.0.0.1', 10800):
        cache_service = CacheService(ignite)
        notification_service = NotificationService(ignite, '127.0.0.1:40400', 'test_agent')
        set_connection(cache_service, notification_service)

        print(get_cache_service())
        print(get_notification_service())

        notification_service.start()
        cache_service.stream_create('test_stream', 'test_instance', 'test_agent', 'test_delegate', 2, True, BoolObject)
        stream = Stream(PerperStream('test_stream', {}, False, False))
        generator = stream.async_generator()

        async def listen_items():
            async for value in generator:
                print(value)

        enter_context('test_instance',  lambda: asyncio.run(listen_items()))

test()