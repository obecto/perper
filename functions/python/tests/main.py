from pyignite import Client

from perper.cache.perper_instance_data import PerperInstanceData
from perper.services.fabric_service import FabricService
from perper.cache.stream_data import StreamData
from perper.services.perper_config import PerperConfig
from perper.model.state import State
from perper.model.context import Context
from perper.services.serializer import Serializer

serializer = Serializer()
ignite = Client()
ignite.connect('localhost', 10800)
ignite.register_binary_type(StreamData)

config = PerperConfig()
fs = FabricService(ignite, config)
instance = PerperInstanceData(ignite, serializer)
state = State(instance, ignite, serializer)
context = Context(instance, fs, state, ignite)

context.stream_action("finc", {1: 2}, None)
print("Reading stream")
print(ignite.get_cache_names())
streams_cache = ignite.get_cache("streams")
for el in streams_cache.scan():
    print(el)