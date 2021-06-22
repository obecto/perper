import uuid
from datetime import datetime

from stream_data import *
from call_data import *
from ignite_cache_extensions import put_if_absent_or_raise, optimistic_update

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

    # STREAMS:

    def stream_create(self, stream, instance, agent, delegate, delegate_type, parameters, parameters_type, ephemeral = True):
        stream_data = create_stream_data(instance, agent, delegate, delegate_type, ephemeral, parameters, parameters_type)
        return put_if_absent_or_raise(self.streams_cache, stream, stream_data)

    def stream_add_listener(self, stream, caller_agent, caller, parameter, filter = {}, replay = False, local_to_data = False):
        stream_listener = create_stream_listener(caller_agent, caller, parameter, replay, local_to_data=local_to_data, filter=filter)

        optimistic_update(self.streams_cache, stream, lambda data: stream_data_add_listener(data, stream_listener))
        return stream_listener

    def stream_remove_listener(self, stream, stream_listener):
        return optimistic_update(self.streams_cache, stream, lambda data: stream_data_remove_listener(data, stream_listener))

    def stream_remove_listener_caller(self, stream, caller, parameter):
        return optimistic_update(self.streams_cache, stream, lambda data: stream_data_remove_listener_caller(data, caller, parameter))

    def stream_write_item(self, stream, item):
        items_cache = self.ignite.get_or_create_cache(stream)
        key = self.get_current_ticks()
        put_if_absent_or_raise(items_cache, key, item)
        return key

    def stream_read_item(self, cache, key):
        items_cache = self.ignite.get_or_create_cache(cache)
        return items_cache.get(key)

    # CALLS:

    def call_create(self, call, instance, agent, delegate, caller_agent, caller, parameters, parameters_type, local_to_data=False,):
        call_data = create_call_data(instance, agent, delegate, caller_agent, caller, local_to_data, parameters, parameters_type)
        return put_if_absent_or_raise(self.calls_cache, call, call_data)

    def call_write_result(self, call, result, result_type):
        return optimistic_update(self.calls_cache, call, lambda data: set_call_data_result(data, result, result_type))

    def call_write_error(self, call, error):
        return optimistic_update(self.calls_cache, call, lambda data: set_call_data_error(data, error))

    def call_write_finished(self, call):
        return optimistic_update(self.calls_cache, call, set_call_data_finished)

    def call_read_error(self, call):
        call_data = self.calls_cache.get(call)
        if hasattr(call_data, 'error'):
            return call_data.error
        else:
            return None

    def call_read_error_and_result(self, call):
        call_data = self.calls_cache.get(call)
        error = call_data.error if hasattr(call_data, 'error') else None
        result = None

        if hasattr(call_data, 'result'):
            result = call_data.result

        return (error, result)
