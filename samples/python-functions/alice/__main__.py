import os
import asyncio

from perper.functions import Perper
from perper.cache.stream_data import ParameterData

os.environ["PERPER_AGENT_NAME"] = "Alice"
os.environ["PERPER_ROOT_AGENT"] = "Alice"

perper = Perper()
context = perper.context

async def launcher():
    agent_name = "Bob"
    caller_agent_name_parameter = "Alice"

    (agent, result_stream) = await context.start_agent(agent_name, {0: caller_agent_name_parameter})
    print(result_stream)

    random_number = await agent.call_function('get_random_number', {0: 1, 1: 1000})
    print(random_number)

asyncio.run(launcher())
