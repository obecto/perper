import os
import asyncio

from perper.functions import Perper
from perper.cache.stream_data import ParameterData

os.environ["PERPER_AGENT_NAME"] = "Bob"

perper = Perper()
context = perper.context

async def Bob(perper_instance, *kwargs):
    stream = context.create_blank_stream(basename='generator')
    print(stream)
    return stream

asyncio.run(perper.listen_triggers({'Bob': Bob}))
