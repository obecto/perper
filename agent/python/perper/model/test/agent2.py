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
notification_service = NotificationService(ignite, '127.0.0.1:40400', 'test_agent2')

set_connection(cache_service, notification_service)
notification_service.start()

async def test(params):
    return ('Started agent 2', String)

async def get_next_agent(params):
    (agent, result) = await enter_context('test_agent2', lambda: start_agent('test_agent3', True, BoolObject))
    return (agent.raw_agent, BinaryObject)

functions = {'test_agent2': test, 'get_next_agent': get_next_agent}
asyncio.run(listen_triggers(ignite, functions))