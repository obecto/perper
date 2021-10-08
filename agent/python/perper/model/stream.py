import random
import sys
import asyncio
import attr
from perper.protocol import StreamDelegateType, PerperStream
from .async_locals import get_cache_service, get_notification_service, get_local_agent, get_instance, get_execution


def create_stream_function(delegate, parameters, ephemeral=True, index=None):
    stream = get_cache_service().generate_name(delegate)
    create_stream(stream, delegate, StreamDelegateType.function, parameters, ephemeral, index)
    return PerperStream(stream)


def create_stream_action(delegate, parameters, ephemeral=True, index=None):
    stream = get_cache_service().generate_name(delegate)
    create_stream(stream, delegate, StreamDelegateType.action, parameters, ephemeral, index)
    return PerperStream(stream)


def create_blank_stream(ephemeral=True, index=None):
    stream = get_cache_service().generate_name("")
    create_stream(stream, "", StreamDelegateType.external, ephemeral, index)
    return PerperStream(stream)


# TODO: Declare stream


def create_stream(stream, delegate, delegate_type, parameters, ephemeral, index):
    index_type = None if index is None else index[0]
    index_fields = None if index is None else index[1]

    get_cache_service().stream_create(stream, get_local_agent(), get_instance(), delegate, delegate_type, parameters, ephemeral, index_type, index_fields)


def stream_modify(stream, **kwargs):
    # if 'filter' in kwargs: kwargs['filter'] = convert_filter(kwargs['filter'])
    return attr.evolve(stream, **kwargs)


async def stream_enumerate(stream, **kwargs):
    stream = stream_modify(stream, **kwargs)
    parameter = random.randrange(0, 10000)  # FIXME
    get_cache_service().perper_stream_add_listener(stream, get_local_agent(), get_instance(), get_execution(), parameter)

    try:
        async for (k, i) in get_notification_service().get_notifications(get_execution(), parameter):
            yield get_cache_service().stream_read_notification(i)
            get_notification_service().consume_notification(k)
    finally:
        get_cache_service().perper_stream_remove_listener(stream, get_execution(), parameter)


async def stream_query(self, type_name, sql_condition, sql_parameters):
    iterator = iter(self.query_sync(type_name, sql_condition, sql_parameters))
    loop = asyncio.get_running_loop()  # via https://stackoverflow.com/a/61774972
    DONE = object()
    while True:
        obj = await loop.run_in_executor(None, next, iterator, DONE)
        if obj is DONE:
            break
        yield obj


def stream_query_sync(self, type_name, sql_condition, sql_parameters):
    def helper(cursor):
        with cursor:
            try:
                while True:
                    yield next(cursor)[0]
            except StopIteration:
                pass

    sql = f'SELECT _VAL FROM "{stream.stream}".{type_name.upper()} {sql_condition}'
    return helper(get_cache_service().stream_query_sql(sql, sql_parameters))
