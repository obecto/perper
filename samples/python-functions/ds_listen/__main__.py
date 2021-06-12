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

asyncio.run(run("-1d0f0a58-3731-4de9-8c07-35c9c5a9d976"))
