import uuid
import attr
from datetime import datetime, timedelta, timezone

from .instance_data import *
from .stream_data import *
from .call_data import *
from .ignite_cache_extensions import put_if_absent_or_raise, optimistic_update
from pyignite.datatypes import CollectionObject, ObjectArrayObject, MapObject


class CacheService:
    def __init__(self, ignite):
        self.ignite = ignite
        self.item_caches = {}

    def start(self):
        self.streams_cache = self.ignite.get_or_create_cache("streams")
        self.calls_cache = self.ignite.get_or_create_cache("calls")
        self.instances_cache = self.ignite.get_or_create_cache("instances")

    def get_current_ticks(self):
        dt = datetime.now(timezone.utc)
        t = (dt - datetime(1970, 1, 1, tzinfo=timezone.utc)) // timedelta(microseconds=1) * 10
        return t

    @classmethod
    def generate_name(cls, basename=None):
        return f"{basename}-{uuid.uuid4()}"

    # INSTANCES:

    def instance_create(self, instance, agent):
        instance_data = create_instance_data(agent)
        return put_if_absent_or_raise(self.instances_cache, instance, instance_data)

    def instance_destroy(self, instance):
        return self.instances_cache.remove_key(instance)

    # STATE:

    def state_set(self, instance, key, value, value_hint=None):
        if instance not in self.item_caches:
            self.item_caches[instance] = self.ignite.get_or_create_cache(instance)
        self.item_caches[instance].put(key, value, value_hint=value_hint)

    def state_get(self, instance, key, default=None):
        if instance not in self.item_caches:
            self.item_caches[instance] = self.ignite.get_or_create_cache(instance)
        result = self.item_caches[instance].get(key)
        if result is None:
            return default
        return result

    # STREAMS:

    def stream_create(self, stream, agent, instance, delegate, delegate_type, parameters, ephemeral=True, index_type=None, index_fields=None):
        stream_data = StreamData(
            instance=instance,
            agent=agent,
            delegate=delegate,
            delegateType=delegate_type,
            parameters=(ObjectArrayObject.OBJECT, parameters),
            ephemeral=ephemeral,
            indexType=index_type,
            indexFields=None if index_fields is None else (MapObject.HASH_MAP, index_fields),
            listeners=(CollectionObject.ARR_LIST, []),
        )
        return put_if_absent_or_raise(self.streams_cache, stream, stream_data)

    def stream_get_parameters(self, stream):
        return self.streams_cache.get(stream).parameters[1]

    def stream_add_listener(self, stream, caller_agent, caller_instance, caller, parameter, filter={}, replay=False, local_to_data=False):
        stream_listener = StreamListener(
            callerAgent=caller_agent,
            callerInstance=caller_instance,
            caller=caller,
            parameter=parameter,
            replay=replay,
            localToData=local_to_data,
            filter=(MapObject.HASH_MAP, filter) if filter != None else None,
        )

        optimistic_update(self.streams_cache, stream, lambda data: attr.evolve(data, listeners=(data.listeners[0], data.listeners[1] + [stream_listener])))

    def stream_remove_listener(self, stream, caller, parameter):
        optimistic_update(
            self.streams_cache,
            stream,
            lambda data: attr.evolve(
                data, listeners=(data.listeners[0], [l for l in data.listeners[1] if not (l.caller == caller and l.parameter == parameter)])
            ),
        )

    def stream_write_item(self, stream, item):
        if stream not in self.item_caches:
            self.item_caches[stream] = self.ignite.get_cache(stream)

        key = self.get_current_ticks()
        put_if_absent_or_raise(self.item_caches[stream], key, item)
        return key

    def stream_read_item(self, cache, key):
        if cache not in self.item_caches:
            self.item_caches[cache] = self.ignite.get_cache(cache)

        return self.item_caches[cache].get(key)

    def stream_query_sql(self, sql, sql_parameters):
        return self.ignite.sql(sql, page_size=1024, query_args=sql_parameters)

    # CALLS:

    def call_create(self, call, agent, instance, delegate, caller_agent, caller, parameters, local_to_data=False):
        call_data = CallData(
            instance=instance,
            agent=agent,
            delegate=delegate,
            parameters=(ObjectArrayObject.OBJECT, parameters),
            callerAgent=caller_agent,
            caller=caller,
            localToData=local_to_data,
            finished=False,
        )
        return put_if_absent_or_raise(self.calls_cache, call, call_data)

    def call_get_parameters(self, call):
        return self.calls_cache.get(call).parameters[1]

    def call_write_result(self, call, result):
        return optimistic_update(self.calls_cache, call, lambda data: attr.evolve(data, finished=True, result=(ObjectArrayObject.OBJECT, result)))

    def call_write_error(self, call, error):
        return optimistic_update(self.calls_cache, call, lambda data: attr.evolve(data, finished=True, error=error))

    def call_write_finished(self, call):
        return optimistic_update(self.calls_cache, call, lambda data: attr.evolve(data, finished=True))

    def call_read_error(self, call):
        return self.calls_cache.get(call).error

    def call_read_error_and_result(self, call):
        call_data = self.calls_cache.get(call)
        error = call_data.error
        result = call_data.result[1] if call_data.result is not None else None

        return (error, result)

    def call_remove(self, call):
        self.calls_cache.remove_key(call)

    # UTILS:

    def perper_stream_remove_listener(self, perper_stream, caller, parameter):
        return self.stream_remove_listener(perper_stream.stream, caller, parameter)

    def perper_stream_add_listener(self, perper_stream, caller_agent, caller_instance, caller, parameter):
        return self.stream_add_listener(
            perper_stream.stream, caller_agent, caller_instance, caller, parameter, perper_stream.filter, perper_stream.replay, perper_stream.localToData
        )

    def stream_read_notification(self, notification):
        return self.stream_read_item(notification.cache, notification.key)

    def call_write_exception(self, call, exception):
        return self.call_write_error(call, str(exception))

    def call_read_result(self, call):
        (error, result) = self.call_read_error_and_result(call)

        if error is not None:
            raise Exception(f"Call failed with error: {error}")

        return result

    def call_check_result(self, call):
        error = self.call_read_error(call)
        if error is not None:
            raise Exception(f"Call failed with error: {error}")
