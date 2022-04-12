import asyncio

from Perper.Extensions import PerperContext, AsyncLocals, PerperStreamExtensions
from System import Boolean, Object

from .context_vars import fabric_service, fabric_execution
from ..protocol.ignite_extensions import create_query_entity, create_query_field
from ..application import task_to_future, convert_async_iterable


async def start_stream(delegate, *parameters, action=False, ephemeral=True, packed=False, index=None):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    builder = _start_stream(delegate, action, ephemeral, packed, index)
    return await task_to_future(lambda _: builder.StartAsync(parameters))


def create_blank_stream(*, ephemeral=True, packed=False, index=None):
    return _start_stream("", False, ephemeral, packed, index)


def declare_stream(delegate, *, action=False, ephemeral=True, packed=False, index=None):
    stream = _start_stream(delegate, action, ephemeral, packed, index)

    async def start(*parameters):
        AsyncLocals.SetConnection(fabric_service.get())
        AsyncLocals.SetExecution(fabric_execution.get())
        await task_to_future(lambda _: stream.StartAsync(parameters))

    return stream.Stream, start


def _start_stream(delegate, action, ephemeral, packed, index):
    stream_builder = PerperContext.Stream(delegate)
    if not ephemeral:
        stream_builder = stream_builder.Persistent()
    if action:
        stream_builder = stream_builder.Action()
    if packed:
        stream_builder = stream_builder.Packed(1)

    return stream_builder


def replay_stream(stream, replay=True, replay_from=0):
    if replay_from:
        return PerperStreamExtensions.Replay(stream, replay_from)
    else:
        return PerperStreamExtensions.Replay(stream, replay)


def local_stream(stream, local_to_data=True):
    return PerperStreamExtensions(stream, local_to_data)


def enumerate_stream(stream, return_type=Object):
    AsyncLocals.SetConnection(fabric_service.get())
    stream_enum = PerperStreamExtensions.EnumerateAsync[return_type](stream).GetAsyncEnumerator()
    return convert_async_iterable(stream_enum)


async def query_stream(stream, type_name, sql_condition, *sql_parameters):
    stream_query = PerperStreamExtensions.Query[Object](stream, type_name, sql_condition, sql_parameters).GetAsyncEnumerator()
    return convert_async_iterable(stream_query)


def destroy_stream(stream):
    fabric_service.get().RemoveExecution(stream.Stream)
    fabric_service.get().RemoveStream(stream.Stream)
