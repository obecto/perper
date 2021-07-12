import random
import sys
from perper.protocol.standard import PerperStream
from perper.protocol.cache_service_extensions import (
    perper_stream_add_listener,
    stream_read_notification,
    perper_stream_remove_listener
)
from .filter_utils import FilterUtils
from .async_locals import *

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

    async def enumerate(self):
        parameter = random.randrange(0, 10000) # TODO: FIXME
        listener = perper_stream_add_listener(get_cache_service(), self.raw_stream, get_local_agent(), get_instance(), parameter)

        try:
            async for (k, i) in get_notification_service().get_notifications(get_instance(), parameter):
                value = stream_read_notification(get_cache_service(), i)
                get_notification_service().consume_notification(k)
                yield value
        finally:
            perper_stream_remove_listener(get_cache_service(), self.raw_stream, listener)
