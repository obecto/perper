import time
import asyncio
import uuid
from pyignite.datatypes.primitive_objects import IntObject
from pyignite.datatypes.standard import String, UUIDObject
from perper.model.context import *
from perper.model.agent import *
from perper.model.async_locals import *
from perper.model.bootstrap import initialize_connection


async def main():
    await initialize_connection("container-sample", True)
    (k, n) = await get_notification_service().get_notification(get_notification_service().CALL, "Startup")
    get_cache_service().call_write_finished(n.call)
    get_notification_service().consume_notification(k)

    r = uuid.uuid4()

    async for (k, n) in get_notification_service().get_notifications(get_notification_service().CALL, "Test"):
        get_cache_service().call_write_result(n.call, [r])
        get_notification_service().consume_notification(k)


asyncio.run(main())
