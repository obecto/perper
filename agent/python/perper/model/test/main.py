import asyncio
from pyignite.datatypes.primitive_objects import IntObject
from pyignite.datatypes.standard import String
from perper.model.context import *
from perper.model.agent import *
from perper.model.bootstrap import initialize

async def generate(count):
    for x in range(count[1][0]):
        await asyncio.sleep(1)
        yield x

async def process_data(num):
    return (num[1][0] + 1, IntObject)

async def main(args):
    stream = stream_function('generate', [5], IntObject)
    
    async for value in stream.enumerate():
        processed_value = await call_function('process_data', [value])
        print(processed_value)
        if processed_value == 10:
            return ('Done', String)

asyncio.run(initialize('test_agent1', {'test_agent1': main, 'generate': generate, 'process_data': process_data}, True))