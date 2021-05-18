import asyncio
from perperapi import PerperApi

perperapi = PerperApi()
context = perperapi.context

generator_stream = context.stream_function('generator', {1:0}, None)

loop = asyncio.get_event_loop()
context = loop.run_until_complete(perperapi.handle_notifications())
