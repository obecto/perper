import asyncio

from Perper.Extensions import PerperContext, AsyncLocals, PerperStreamExtensions
from System import Boolean, Object

from .context_vars import fabric_service, fabric_execution
from ..protocol.ignite_extensions import create_query_entity, create_query_field
from ..application import task_to_future


async def start_stream(delegate, *parameters, action=False, ephemeral=True, packed=False, index=None):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    builder = _start_stream(delegate, action, ephemeral, packed, _index_to_query_entities(index))
    return await task_to_future(builder.StartAsync(parameters))


def create_blank_stream(*, ephemeral=True, packed=False, index=None):
    return _start_stream("", False, ephemeral, packed, _index_to_query_entities(index))


def declare_stream(delegate, *, action=False, ephemeral=True, packed=False, index=None):
    stream = _start_stream(delegate, action, ephemeral, packed, _index_to_query_entities(index))

    async def start(*parameters):
        AsyncLocals.SetConnection(fabric_service.get())
        AsyncLocals.SetExecution(fabric_execution.get())
        await task_to_future(stream.StartAsync(parameters))

    return stream.Stream, start


def _index_to_query_entities(index):
    def convert(i):
        if isinstance(i, tuple):
            (type_name, type_fields) = i
            if isinstance(type_fields, dict):
                type_fields = type_fields.items()
            return create_query_entity(
                value_type_name=type_name, query_fields=[create_query_field(field_name, field_type) for (field_name, field_type) in type_fields]
            )
        return i

    if index is None:
        return []
    elif isinstance(index, list):
        return [convert(i) for i in index]
    else:
        return [convert(index)]


def _start_stream(delegate, action, ephemeral, packed, query_entities):
    stream_builder = PerperContext.Stream(delegate)
    if not ephemeral:
        stream_builder = stream_builder.Persistent()
    if action:
        stream_builder = stream_builder.Action()
    if packed:
        stream_builder = stream_builder.Packed(1)

    return stream_builder

#
# def replay_stream(stream, replay=True, replay_from=0):
#     return attr.evolve(stream, startIndex=-1 if not replay else replay_from)
#
#
# def local_stream(stream, local_to_data=True):
#     return attr.evolve(stream, localToData=local_to_data)


async def enumerate_stream(stream, return_type=Object):
    stream_enum = PerperStreamExtensions.EnumerateAsync[return_type](stream).GetAsyncEnumerator()
    while True:
        AsyncLocals.SetConnection(fabric_service.get())
        if not await task_to_future(stream_enum.MoveNextAsync(), value_task=True):
            break
        yield stream_enum.Current


async def query_stream(stream, type_name, sql_condition, *sql_parameters):
    iterator = iter(query_stream_sync(stream, type_name, sql_condition, *sql_parameters))
    loop = asyncio.get_running_loop()  # via https://stackoverflow.com/a/61774972
    DONE = object()
    while True:
        obj = await loop.run_in_executor(None, next, iterator, DONE)
        if obj is DONE:
            break
        yield obj


def query_stream_sync(stream, type_name, sql_condition, *sql_parameters):
    def helper(cursor):
        with cursor:
            try:
                while True:
                    yield next(cursor)[0]
            except StopIteration:
                pass

    sql = f'SELECT _VAL FROM "{stream.stream}".{type_name.upper()} {sql_condition}'
    return helper(fabric_service.get().query_stream_sql(sql, sql_parameters))


def destroy_stream(stream):
    fabric_service.get().RemoveExecution(stream.Stream)
    fabric_service.get().RemoveStream(stream.Stream)
