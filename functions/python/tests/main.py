from pyignite import Client

from cache.instance_data import PerperInstanceData
from cache.stream_data import StreamData
from model.state import State
from model.context import Context
from services.serializer import Serializer

serializer = Serializer()
ignite = Client()
ignite.connect('localhost', 10800)
ignite.register_binary_type(StreamData)

instance = PerperInstanceData(ignite, serializer)
state = State(instance, ignite, serializer)
context = Context(instance, None, state, ignite)

context.stream_action()
print("Reading stream")
print(ignite.get_cache_names())
streams_cache = ignite.get_cache("streams")
for el in streams_cache.scan():
    print(el)