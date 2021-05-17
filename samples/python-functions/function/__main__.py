import asyncio
from perperapi import perperapi

loop = asyncio.get_event_loop()
result = loop.run_until_complete(perperapi())