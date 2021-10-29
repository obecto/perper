import asyncio
from pyignite.datatypes.primitive_objects import BoolObject
from pyignite.datatypes.standard import String
from pyignite.datatypes.complex import BinaryObject
from perper.model.context import *
from perper.model.bootstrap import initialize

async def test(params):
    return ('Started agent 3', String)

async def get_stream(params):
    stream = stream_function('generate', True)
    return (stream.raw_stream, BinaryObject)

async def generate(params):
    for x in range(10):
        await asyncio.sleep(1)
        yield x

asyncio.run(initialize('test_agent3', {'test_agent3': test, 'get_stream': get_stream, 'generate': generate}))