import os
import random
import asyncio

from perper.functions import Perper
from perper.model import Stream

os.environ["PERPER_AGENT_NAME"] = "Application"

perper = Perper()
context = perper.context

async def run(stream_name):
    stream = Stream(streamname=stream_name)
    stream.set_parameters(context.ignite, context.fabric, instance=context.instance, serializer=context.serializer, state=context.state)

    async_gen = await stream.get_async_generator()
    async for item in async_gen:
        print(item)

asyncio.run(run("-b75245f5-33c6-4464-acc3-666f838f3b1e"))
