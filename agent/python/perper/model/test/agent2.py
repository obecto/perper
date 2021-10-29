import asyncio
from pyignite.datatypes.primitive_objects import BoolObject
from pyignite.datatypes.standard import String
from pyignite.datatypes.complex import BinaryObject
from perper.model.context import *
from perper.model.bootstrap import initialize

async def test(params):
    return ('Started agent 2', String)

async def get_next_agent(params):
    (agent, result) = await start_agent('test_agent3', True, BoolObject)
    return (agent.raw_agent, BinaryObject)

asyncio.run(initialize('test_agent2', {'test_agent2': test, 'get_next_agent': get_next_agent}))