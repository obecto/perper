from perper.model.filter_utils import FilterUtils
from perper.cache.stream_listener import StreamListener
from pyignite import GenericObjectMeta
from collections import OrderedDict
from pyignite.datatypes import *
from perper.cache.notifications import StreamItemNotification


class Stream(
    metaclass=GenericObjectMeta,
    type_name="PerperStream`1[[SimpleData]]",
    schema=OrderedDict([("streamname", String)]),
):
    def set_parameters(self, ignite, fabric, **kwargs):
        self.fabric = fabric
        self.ignite = ignite
        self._ignite = ignite
        self.stream_name = self.streamname

        if len(kwargs) > 0:
            self.set_additional_parameters(kwargs)

        self.function_name = None

    def set_additional_parameters(self, kwargs):
        self.serializer = kwargs["serializer"]
        self.state = kwargs["state"]
        self.instance = kwargs["instance"]
        self.parameter_index = self.instance.get_stream_parameter_index()

    def get_enumerable(self, _filter, replay, local_to_data):
        enumerable = StreamEnumerable(self, _filter, replay, local_to_data)
        return enumerable

    async def get_async_generator(
        self,
    ):
        return self.get_enumerable({}, False, False).async_generator()

    def data_local(
        self,
    ):
        return self.get_enumerable({}, False, True)

    def _filter(self, _filter, data_local=False):
        return self.get_enumerable(
            FilterUtils.convert_filter(_filter), False, data_local
        )

    def replay(self, data_local=False, **kwargs):
        if len(kwargs > 0):
            return self.get_enumerable(
                FilterUtils.convert_filter(kwargs["filter"]), True, data_local
            )
        else:
            return self.get_enumerable({}, True, data_local)

    def query(self, query):
        cache = self.ignite.get_cache(self.stream_name)
        queryable = query([pair.value for pair in cache.scan()])

        for (index, value) in enumerate(queryable):
            yield self.serializer.deserialize(value)


class StreamEnumerable:
    def __init__(self, stream, _filter, local_to_data, replay):
        self._stream = stream
        self.local_to_data = local_to_data
        self._filter = _filter
        self.replay = replay

    async def async_generator(
        self,
    ):
        try:
            self.__add_listener()
            async for (key, notification) in self._stream.fabric.get_notifications():
                if type(notification).__name__ == StreamItemNotification.__name__:
                    cache = self._stream.ignite.get_cache(notification.cache)
                    value = cache.get(notification.key)
                    try:
                        yield self._stream.serializer.deserialize(value)
                    finally:
                        self._stream.fabric.consume_notification(key)
        finally:
            if self._stream.parameter_index < 0:
                self.__remove_listener()

    def __modify_stream_data(self, modification):
        streams_cache = self._stream.ignite.get_cache("streams")
        current_value = streams_cache.get(self._stream.stream_name)
        new_value = modification(current_value)
        # This should be replace
        streams_cache.put(self._stream.stream_name, new_value)

    def __add_listener(self):
        stream_listener = StreamListener(
            agentdelegate=self._stream.fabric.agent_delegate,
            stream=self._stream.instance.instance_name,
            parameter=self._stream.parameter_index,
            filter=(1, self._filter),
            replay=self.replay,
            localtodata=self.local_to_data,
        )

        def __add_listener_lambda_(stream_data):
            listeners = getattr(
                stream_data, "listeners", getattr(stream_data, "Listeners", None)
            )
            listeners[1].append(stream_listener)
            return stream_data

        return self.__modify_stream_data(__add_listener_lambda_)

    def __remove_listener(
        self,
    ):
        return self.__modify_stream_data(self.remove_listener_lambda)

    def __remove_listener_lambda(self, stream_data):
        current_listeners = stream_data.listeners
        bin_obj = BinaryObject()
        for listener in current_listeners:
            if (
                (listener is bin_obj)
                and (bin_obj.stream == _stream._instance.instance_name)
                and (bin_obj.parameter == _stream.kwargs["parameter_index"])
            ):
                current_listeners.remove(listener)
                break

        builder = stream_data.to_builder()
        builder.listeners = current_listeners
        return builder.build()
