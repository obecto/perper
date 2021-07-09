import asyncio
from pyignite.datatypes.primitive_objects import IntObject
from pyignite.datatypes.standard import String
from perper.model.context import *
from perper.model.agent import *
from perper.model.bootstrap import initialize

async def generate(count):
    for x in range(count):
        await asyncio.sleep(1)
        yield x

async def process_data(num):
    return (num + 1, IntObject)

async def main():
    stream = stream_function('generate', [5])
    
    async for value in stream.enumerate():
        processed_value = await call_function('process_data', [value])
        print(processed_value)
        if processed_value == 10:
            return ('Done', String)

asyncio.run(initialize('test_agent1', {'Startup': main, 'generate': generate, 'process_data': process_data}, True))