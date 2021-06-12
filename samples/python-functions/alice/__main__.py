import os
import asyncio

from perper.functions import Perper
from perper.cache.stream_data import ParameterData
from perper.model import Stream

from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String

os.environ["PERPER_AGENT_NAME"] = "Alice"
os.environ["PERPER_ROOT_AGENT"] = "Alice"

perper = Perper()
context = perper.context

class SimpleStream(Stream, 
    metaclass=GenericObjectMeta,
    type_name="PerperStream`1[[SimpleData]]",
    schema=OrderedDict([("streamname", String)])
):
    pass

perper.register_stream_class(SimpleStream)

async def launcher():
    agent_name = "Bob"
    caller_agent_name_parameter = "Alice"

    (agent, result_stream) = await context.start_agent(agent_name, {0: caller_agent_name_parameter})
    print('Stream passed through start_agent:', result_stream)

    random_number = await agent.call_function('get_random_number', {0: 1, 1: 1000})
    print('Random number:', random_number)

    await agent.call_action('log_something', {0: 'SOMETHING HAS BEEN LOGGED!'})

asyncio.run(launcher())
