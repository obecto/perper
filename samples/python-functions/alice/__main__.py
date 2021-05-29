import os
import asyncio

from perper.functions import Perper
from perper.cache.stream_data import ParameterData

perper = Perper()
context = perper.context

os.environ["PERPER_AGENT_NAME"] = "Alice"
os.environ["PERPER_ROOT_AGENT"] = "Alice"

async def launcher():
    agent_name = 'Bob'
    caller_agent_name_parameter = 'Alice'

    (agent, result) = await context.start_agent(agent_name, {0: caller_agent_name_parameter})
    print(result)

asyncio.run(launcher())
