import asyncio
from pyignite.datatypes.primitive_objects import BoolObject
from perper.model.context import *
from perper.model.agent import Agent
from perper.model.stream import Stream
from perper.model.bootstrap import initialize
from pyignite.datatypes.standard import String

async def main(*args):
    (agent2, result) = await start_agent('test_agent2', True, BoolObject)
    print(agent2)
    
    perper_agent3 = await agent2.call_function('get_next_agent', True, BoolObject)
    print(perper_agent3)
    agent3 = Agent(perper_agent3)
    print(agent3)
    
    inc_stream = await agent3.call_function('get_stream', True, BoolObject)
    csv_contents = ''
    async for v in Stream(inc_stream).enumerate():
        print(v)
        if v == ',,':
            print('Finished!')
            with open('inc.csv', 'w') as f:
                f.write(csv_contents)
            return ('Done', String)
        
        csv_contents += v

asyncio.run(initialize('test_agent1', {'test_agent1': main}, True))