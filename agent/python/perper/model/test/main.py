import asyncio
from pyignite.datatypes.primitive_objects import BoolObject
from perper.model.context import *
from perper.model.agent import Agent
from perper.model.stream import Stream
from pyignite.datatypes.standard import String
from perper.model.bootstrap import initialize

async def generate(params):
    for x in range(10):
        await asyncio.sleep(1)
        yield x

async def main(sdgsdg):
    stream = stream_function('generate', True, BoolObject)
    print(stream)
    
    async for value in stream.enumerate():
        print(value)
        if value == 9:
            return ('Done', String)

asyncio.run(initialize('test_agent1', {'test_agent1': main, 'generate': generate}, True))