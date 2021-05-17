from pyignite import Client

from perper.cache.perper_instance_data import PerperInstanceData
from perper.services.fabric_service import FabricService
from perper.cache.stream_data import StreamData
from perper.services.perper_config import PerperConfig
from perper.model.state import State
from perper.model.context import Context
from perper.services.serializer import Serializer
from perper.utils.perper_thin_client import PerperThinClient


# TODO: Move it under functions/python
async def perperapi():
    serializer = Serializer()
    ignite = PerperThinClient()
    ignite.compact_footer = True
    ignite.connect('localhost', 10800)

    config = PerperConfig()
    fs = FabricService(ignite, config)
    fs.start()

    instance = PerperInstanceData(ignite, serializer)
    state = State(instance, ignite, serializer)
    context = Context(instance, fs, state, ignite)

    async for (k, n) in fs.get_notifications():
        print(n)
    
        # if isinstance(n, StreamItemNotification):
        #     cache = ignite.get_cache(n.cache)
        #     item = cache.get(n.key)
        #     print(f"Item received from [{n.cache}]: " + str(item))
        #     fs.consume_notification(k)
        pass

    return context

