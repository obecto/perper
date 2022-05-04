import asyncio

from ..bindings import task_to_future, convert_async_iterable, with_async_locals, restore_async_locals

from System import Object
from Perper.Extensions import PerperContext, PerperStreamExtensions


async def start_stream(delegate, *parameters, action=False, ephemeral=True, packed=False, index=None):
    builder = _create_stream_builder(delegate, action, ephemeral, packed, index)
    return await task_to_future(with_async_locals(lambda _: builder.StartAsync(parameters)))


async def create_blank_stream(*, ephemeral=True, packed=False, index=None):
    builder = _create_stream_builder("", False, ephemeral, packed, index)
    return await task_to_future(with_async_locals(lambda _: builder.StartAsync()))


def declare_stream(delegate, *, action=False, ephemeral=True, packed=False, index=None):
    stream = _create_stream_builder(delegate, action, ephemeral, packed, index)

    async def start(*parameters):
        await task_to_future(with_async_locals(lambda _: stream.StartAsync(parameters)))

    return stream.Stream, start


def _create_stream_builder(delegate, action, ephemeral, packed, index):
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
    restore_async_locals()
    stream_enum = PerperStreamExtensions.EnumerateAsync[return_type](stream).GetAsyncEnumerator()
    return convert_async_iterable(stream_enum)


def enumerate_stream_with_keys(stream, return_type=Object):
    restore_async_locals()
    stream_enum = PerperStreamExtensions.EnumerateWithKeysAsync[return_type](stream).GetAsyncEnumerator()
    return convert_async_iterable(stream_enum)


async def query_stream(stream, type_name, sql_condition, *sql_parameters):
    restore_async_locals()
    stream_query = PerperStreamExtensions.Query[Object](stream, type_name, sql_condition, sql_parameters).GetAsyncEnumerator()
    return convert_async_iterable(stream_query)


async def destroy_stream(stream):
    return await task_to_future(with_async_locals(lambda _: PerperStreamExtensions.DestroyAsync(stream)))
