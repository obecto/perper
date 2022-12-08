import random
import sys
import asyncio
import attr

from ..protocol.proto.grpc2_model_pb2 import PerperExecution, PerperInstance, PerperStream
from .context_vars import fabric_service, fabric_execution


async def start_stream(delegate, *parameters, action=False, ephemeral=True, packed=False, index=None):
    return await _start_stream(delegate, parameters, action, ephemeral, packed, _index_to_query_entities(index))


async def create_blank_stream(*, ephemeral=True, packed=False, index=None):
    return await _start_stream("", None, False, ephemeral, packed, _index_to_query_entities(index))


async def declare_stream(delegate, *, action=False, ephemeral=True, packed=False, index=None):
    stream = await _start_stream(delegate, None, action, ephemeral, packed, _index_to_query_entities(index))

    async def start(*parameters):
        await fabric_service.get().create_execution(
            PerperInstance(
                agent=fabric_execution.get().agent,
                instance=fabric_execution.get().instance,
            ),
            f"{delegate}-stream",
            parameters,
        )

    return (stream, start)


def _index_to_query_entities(index):
    if index is not None:
        raise NotImplementedError("Querying not implemented yet for GRPC")

    return None
    # def convert(i):
    #     if isinstance(i, tuple):
    #         (type_name, type_fields) = i
    #         if isinstance(type_fields, dict):
    #             type_fields = type_fields.items()
    #         return create_query_entity(
    #             value_type_name=type_name, query_fields=[create_query_field(field_name, field_type) for (field_name, field_type) in type_fields]
    #         )
    #     return i

    # if index is None:
    #     return []
    # elif isinstance(index, list):
    #     return [convert(i) for i in index]
    # else:
    #     return [convert(index)]


async def _start_stream(delegate, parameters, action, ephemeral, packed, query_entities):
    stream_base_name = fabric_service.get().generate_name(delegate)
    stream = PerperStream(stream=stream_base_name, start_key=-1, stride=1)

    await fabric_service.get().create_stream(
        stream=stream,
        ephemeral=ephemeral,
        action=action,
        options=None,
        query_entities=query_entities,
    )

    if not ephemeral:
        await fabric_service.get().set_stream_listener_position(
            f"{stream_base_name}-persist",
            stream,
            fabric_service.get().LISTENER_PERSIST_ALL,
        )
    elif action:
        await fabric_service.get().set_stream_listener_position(
            f"{stream_base_name}-trigger",
            stream,
            fabric_service.get().LISTENER_JUST_TRIGGER,
        )

    if parameters != None:
        await fabric_service.get().create_execution(
            PerperInstance(
                agent=fabric_execution.get().agent,
                instance=fabric_execution.get().instance,
            ),
            f"{delegate}-stream",
            parameters,
        )

    return stream


def replay_stream(stream, replay=True, replay_from=0):
    return attr.evolve(stream, startIndex=-1 if not replay else replay_from)


def local_stream(stream, local_to_data=True):
    return attr.evolve(stream, localToData=local_to_data)


# def filter_stream(stream, filter):
#    return attr.evolve(stream, filter = filter)


async def enumerate_stream(stream):
    async for _, item in enumerate_stream_with_keys(stream):
        yield item


async def enumerate_stream_with_keys(stream):
    listener = fabric_service.get().generate_name(stream.stream)  # TODO: keep state while iterating
    stream_with_keys = await fabric_service.get().enumerate_stream_items(stream.stream, stream.startIndex, stream.stride, stream.localToData)

    try:
        async for (key, value) in stream_with_keys:
            yield (key, value)
    finally:
        await fabric_service.get().remove_stream_listener(stream, listener)


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


async def destroy_stream(stream):
    # TODO Potentially hanging execution?
    await fabric_service.get().remove_stream(stream)
