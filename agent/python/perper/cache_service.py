import uuid
from datetime import datetime

from stream_data import create_stream_data, create_stream_listener, stream_data_add_listener, stream_data_remove_listener

class CacheService:
    def __init__(self, ignite):
        self.ignite = ignite
        self.streams_cache = ignite.get_or_create_cache('streams')
        self.calls_cache = ignite.get_or_create_cache('calls')

    def get_current_ticks(self):
        dt = datetime.utcnow()
        t = (dt - datetime(1, 1, 1)).total_seconds() * 10000000
        return t

    def generate_name(self, basename=None):
        return f"{basename}-{uuid.uuid4()}"

    # STREAM:

    def stream_create(self, stream, instance, agent, delegate, delegate_type, parameters, parameters_type, ephemeral = True):
        stream_data = create_stream_data(instance, agent, delegate, delegate_type, ephemeral, parameters, parameters_type)
        return self.ignite.put_if_absent_or_raise(self.streams_cache, stream, stream_data)


    def stream_add_listener(self, stream, caller_agent, caller, parameter, filter = {}, replay = False, local_to_data = False):
        stream_listener = create_stream_listener(caller_agent, caller, parameter, replay, local_to_data=local_to_data, filter=filter)

        self.ignite.optimistic_update(self.streams_cache, stream, lambda data: stream_data_add_listener(data, stream_listener))
        return stream_listener

    def stream_remove_listener(self, stream, stream_listener): # TODO: Implement StreamRemoveListener(string stream, string caller, int parameter)
        return self.ignite.optimistic_update(self.streams_cache, stream, lambda data: stream_data_remove_listener(data, stream_listener))

    def stream_write_item(self, stream, item):
        items_cache = self.ignite.get_or_create_cache(stream)
        key = self.get_current_ticks()
        self.ignite.put_if_absent_or_raise(items_cache, key, item)
        return key

    def stream_read_item(self, cache, key):
        items_cache = self.ignite.get_or_create_cache(cache)
        return items_cache.get(key)
