import asyncio
from perper.protocol.thin_client import PerperIgniteClient

from perper.protocol.cache_service import CacheService
from perper.protocol.notification_service import NotificationService
from async_locals import AsyncLocals

ignite = PerperIgniteClient()
async def test():
    with ignite.connect('127.0.0.1', 10800):
        cache_service = CacheService(ignite)
        notification_service = NotificationService(ignite, '127.0.0.1:40400', 'test_agent')
        AsyncLocals.set_connection(cache_service, notification_service)

        print(AsyncLocals.get_cache_service())
        print(AsyncLocals.get_notification_service())

asyncio.run(test())