import os
import asyncio
import traceback
import functools
import backoff
from collections.abc import AsyncIterable, Awaitable
from pyignite import Client
from pyignite.utils import is_hinted
from pyignite.exceptions import ReconnectError
from grpc import RpcError
from .async_locals import enter_context, get_cache_service, get_execution, get_notification_service, set_connection, set_context
from .task_collection import TaskCollection
from perper.protocol import CacheService, NotificationService

def initialize_connection(agent, use_instance=False):
    instance = os.getenv('X_PERPER_INSTANCE') if use_instance else None
    (ignite_address, ignite_port) = os.getenv('APACHE_IGNITE_ENDPOINT', '127.0.0.1:10800').split(':')
    grpc_endpoint = os.getenv('PERPER_FABRIC_ENDPOINT', '127.0.0.1:40400')
    print(f"APACHE_IGNITE_ENDPOINT: {ignite_address}:{ignite_port}")
    print(f"PERPER_FABRIC_ENDPOINT: {grpc_endpoint}")
    if use_instance:
        print(f"X_PERPER_INSTANCE: {instance}")


    ignite = Client()
    ignite_port = int(ignite_port)

    cache_service = CacheService(ignite)
    notification_service = NotificationService(ignite, grpc_endpoint, agent, instance)

    set_connection(cache_service, notification_service) # It is important that this call occurs in a sync context

    @backoff.on_exception(backoff.expo, ReconnectError, on_backoff=(lambda x: print(f"Failed to connect to Ignite, retrying in {x['wait']:0.1f}s")))
    def connect_ignite():
        ignite.connect(ignite_address, ignite_port)
        cache_service.start()

    @backoff.on_exception(backoff.expo, RpcError, on_backoff=(lambda x: print(f"Failed to connect to GRPC, retrying in {x['wait']:0.1f}s")))
    async def connect_grpc():
        await notification_service.start()

    async def connect_helper():
        connect_ignite()
        await connect_grpc()

    return asyncio.create_task(connect_helper())

async def initialize(agent, calls = {}, streams = {}, use_instance=False):
    await initialize_connection(agent, use_instance)

    task_collection = TaskCollection()

    if 'Init' in calls:
        init_function = calls.pop('Init')
        async def invoke_init():
            try:
                await enter_context(agent + '-Init', 'Init-Init', init_function)
            except Exception as ex:
                print("Error while invoking Init:")
                traceback.print_exception(type(ex), ex, ex.__traceback__)
        task_collection.add(invoke_init())

    for (delegate, function) in calls.items():
        task_collection.add(listen_call_triggers(task_collection, delegate, function))
    for (delegate, function) in streams.items():
        task_collection.add(listen_stream_triggers(task_collection, delegate, function))

    try:
        await task_collection
    finally:
        await get_notification_service().stop()

def initialize_notebook(agent = None):
    if agent is None: agent = CacheService.generate_name('notebook')

    task_collection = TaskCollection()

    task_collection.add(initialize_connection(agent))
    set_context(agent + '-Init', 'Init-Init')

    return task_collection

async def listen_call_triggers(task_collection, delegate, function):
    async for (k, n) in get_notification_service().get_notifications(NotificationService.CALL, delegate):
        task_collection.add(process_notification(k, n.instance, n.call, delegate, functools.partial(process_call, function)))

async def listen_stream_triggers(task_collection, delegate, function):
    async for (k, n) in get_notification_service().get_notifications(NotificationService.STREAM, delegate):
        task_collection.add(process_notification(k, n.instance, n.stream, delegate, functools.partial(process_stream, function)))

async def process_notification(key, instance, execution, delegate, processor):
    try:
        await enter_context(instance, execution, processor)
    except Exception as ex:
        print("Error while invoking", delegate, ":")
        traceback.print_exception(type(ex), ex, ex.__traceback__)
    get_notification_service().consume_notification(key)

async def process_call(function):
    call = get_execution()

    parameters = get_cache_service().call_get_parameters(call)
    result = function(*parameters)

    if isinstance(result, Awaitable):
        result = await result

    if result is None:
        get_cache_service().call_write_finished(call)
    if isinstance(result, tuple) and not is_hinted(result):
        get_cache_service().call_write_result(call, list(result))
    else:
        get_cache_service().call_write_result(call, [result])

async def process_stream(function):
    stream = get_execution()

    parameters = get_cache_service().stream_get_parameters(stream)
    result = function(*parameters)

    if isinstance(result, Awaitable):
        result = await result

    if isinstance(result, AsyncIterable):
        async for data in result:
            get_cache_service().stream_write_item(stream, data)
