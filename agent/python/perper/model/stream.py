from perper.protocol.standard import PerperStream
from perper.protocol.cache_service_extensions import perper_stream_add_listener
from .filter_utils import FilterUtils
from .async_locals import AsyncLocals

class Stream:
    def __init__(self, raw_stream):
        self.raw_stream = raw_stream
    
    # Stream<T>

    def data_local(self):
        return Stream(PerperStream(self.raw_stream.stream, self.raw_stream.filter, self.raw_stream.replay, True))

    def filter(self, filter, data_local = False):
        return Stream(PerperStream(self.raw_stream.stream, FilterUtils.convert_filter(filter), self.raw_stream.replay, data_local))

    def replay(self, data_local=False):
        return Stream(PerperStream(self.raw_stream.stream, self.raw_stream.filter, True, data_local))

    def replay_filter(self, filter, data_local=False):
        return Stream(PerperStream(self.raw_stream.stream, FilterUtils.convert_filter(filter), True, data_local))

    def filter(self):
        pass

    async def async_generator(self):
        parameter = True # TODO: FIXME
        listener = perper_stream_add_listener(AsyncLocals.get_cache_service(), self.raw_stream, AsyncLocals.get_agent(), AsyncLocals.get_instance(), parameter)

        async for (k, i) in AsyncLocals.get_notification_service().get_notifications(AsyncLocals.get_instance(), parameter):
            yield (k, i)
            AsyncLocals.get_notification_service().consume_notification(k)