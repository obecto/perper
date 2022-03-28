import random
import sys
import asyncio
import attr
from ..model import PerperStream
from ..protocol.ignite_extensions import create_query_entity, create_query_field
from .context_vars import fabric_service, fabric_execution


def start_stream(delegate, *parameters, action=False, ephemeral=True, packed=False, index=None):
    return _start_stream(delegate, parameters, action, ephemeral, packed, _index_to_query_entities(index))


def create_blank_stream(*, ephemeral=True, packed=False, index=None):
    return _start_stream("", None, False, ephemeral, packed, _index_to_query_entities(index))


def declare_stream(delegate, *, action=False, ephemeral=True, packed=False, index=None):
    stream = _start_stream(delegate, None, action, ephemeral, packed, _index_to_query_entities(index))

    def start(*parameters):
        fabric_service.get().create_execution(stream.stream, fabric_execution.get().agent, fabric_execution.get().instance, delegate, parameters)

    return (stream, start)


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


def _start_stream(delegate, parameters, action, ephemeral, packed, query_entities):
    stream = fabric_service.get().generate_name(delegate)

    fabric_service.get().create_stream(stream, query_entities)

    if not ephemeral:
        fabric_service.get().set_stream_listener_position(f"{stream}-persist", stream, fabric_service.get().LISTENER_PERSIST_ALL)
    elif action:
        fabric_service.get().set_stream_listener_position(f"{stream}-trigger", stream, fabric_service.get().LISTENER_JUST_TRIGGER)

    if parameters != None:
        fabric_service.get().create_execution(stream, fabric_execution.get().agent, fabric_execution.get().instance, delegate, parameters)

    return PerperStream(stream=stream, startIndex=-1, stride=1 if packed else 0, localToData=False)


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

    keys = await fabric_service.get().enumerate_stream_item_keys(stream.stream, stream.startIndex, stream.stride, stream.localToData)
    fabric_service.get().set_stream_listener_position(listener, stream.stream, fabric_service.get().LISTENER_PERSIST_ALL)

    try:
        async for key in keys:
            fabric_service.get().set_stream_listener_position(listener, stream.stream, key)
            yield (key, fabric_service.get().read_stream_item(stream.stream, key))
    finally:
        fabric_service.get().remove_stream_listener(listener)


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
    fabric_service.get().remove_execution(stream.stream)
    fabric_service.get().remove_stream(stream.stream)
