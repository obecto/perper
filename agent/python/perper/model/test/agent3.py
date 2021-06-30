import asyncio

from pyignite import Client
from pyignite.datatypes.primitive_objects import BoolObject
from pyignite.datatypes.standard import String
from pyignite.datatypes.complex import BinaryObject
from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService
from perper.model.context import *
from perper.model.async_locals import enter_context

from api_handler import listen_triggers

ignite = Client()
ignite.connect('127.0.0.1', 10800)

cache_service = CacheService(ignite)
notification_service = NotificationService(ignite, '127.0.0.1:40400', 'test_agent3')

set_connection(cache_service, notification_service)
notification_service.start()

async def test(params):
    return ('Started agent 3', String)

async def get_stream(params):
    stream = enter_context('test_agent3', lambda: stream_function('generate', True, BoolObject))
    return (stream.raw_stream, BinaryObject)

async def generate(params):
    for x in range(10):
        await asyncio.sleep(1)
        yield x

functions = {'test_agent3': test, 'get_stream': get_stream, 'generate': generate}
asyncio.run(listen_triggers(ignite, functions))