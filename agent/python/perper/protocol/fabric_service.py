import uuid
import attr
import time
import asyncio
import grpc
import warnings
from datetime import datetime, timedelta, timezone
from collections import defaultdict, namedtuple

from pyignite.datatypes import ObjectArrayObject, prop_codes

from .cache import InstanceData, ExecutionData, StreamListener
from .task_collection import TaskCollection
from .proto import fabric_pb2, fabric_pb2_grpc
from .ignite_extensions import put_if_absent_or_raise, optimistic_update


FabricExecution = namedtuple("Execution", ["agent", "instance", "delegate", "execution"])


class factorydict(dict):
    def __init__(self, default_factory):
        self.default_factory = default_factory

    def __missing__(self, key):
        value = self.default_factory(key)
        self[key] = value
        return value


class FabricService:
    CANCELLED = object()

    def __init__(self, ignite, grpc_channel):
        self.task_collection = TaskCollection()

        self.ignite = ignite
        self.executions_cache = self.ignite.get_or_create_cache("executions")
        self.stream_listeners_cache = self.ignite.get_or_create_cache("stream-listeners")
        self.instances_cache = self.ignite.get_or_create_cache("instances")

        self.item_caches = factorydict(self.ignite.get_cache)
        self.state_caches = factorydict(self.ignite.get_or_create_cache)

        self.grpc_channel = grpc_channel
        self.fabric_stub = fabric_pb2_grpc.FabricStub(self.grpc_channel)

        self.execution_channels = defaultdict(asyncio.Queue)
        self.execution_listen_tasks = factorydict(self._create_executions_listener)
        self.execution_tasks = {}

    @classmethod
    def get_current_ticks(cls):
        dt = datetime.now(timezone.utc)
        t = (dt - datetime(1970, 1, 1, tzinfo=timezone.utc)) // timedelta(microseconds=1) * 10
        return t

    @classmethod
    def generate_name(cls, basename=None):
        return f"{basename}-{uuid.uuid4()}"

    async def __aenter__(self):
        return self

    async def __aexit__(self, *args):
        await self.stop()

    async def stop(self):
        self.task_collection.cancel()
        # print(self.task_collection.tasks)
        await self.task_collection.wait()
        await self.grpc_channel.close()

    # INSTANCES:

    def create_instance(self, instance, agent):
        return self.create_execution(instance, "Registry", agent, "Run", [])
        # instance_data = InstanceData(agent=agent)
        # return put_if_absent_or_raise(self.instances_cache, instance, instance_data)

    def remove_instance(self, instance):
        return self.remove_execution(instance)
        # return self.instances_cache.remove_key(instance)

    # EXECUTIONS:

    def create_execution(self, execution, agent, instance, delegate, parameters):
        execution_data = ExecutionData(
            instance=instance,
            agent=agent,
            delegate=delegate,
            parameters=(ObjectArrayObject.OBJECT, parameters),
            finished=False,
        )
        return put_if_absent_or_raise(self.executions_cache, execution, execution_data)

    def remove_execution(self, execution):
        self.executions_cache.remove_key(execution)

    def write_execution_finished(self, execution):
        return optimistic_update(self.executions_cache, execution, lambda data: attr.evolve(data, finished=True))

    def write_execution_result(self, execution, result):
        return optimistic_update(self.executions_cache, execution, lambda data: attr.evolve(data, finished=True, result=(ObjectArrayObject.OBJECT, result)))

    def write_execution_error(self, execution, error):
        return optimistic_update(self.executions_cache, execution, lambda data: attr.evolve(data, finished=True, error=error))

    def read_execution_parameters(self, execution):
        return self.executions_cache.get(execution).parameters[1]

    def read_execution_error(self, execution):
        return self.executions_cache.get(execution).error

    def read_execution_error_and_result(self, execution):
        execution_data = self.executions_cache.get(execution)
        error = execution_data.error
        result = execution_data.result[1] if execution_data.result is not None else None

        return (error, result)

    # STATE:

    def set_state_value(self, instance, key, value, value_hint=None):
        self.state_caches[instance].put(key, value, value_hint=value_hint)

    def remove_state_value(self, instance, key):
        self.state_caches[instance].remove_key(key)

    def get_state_value(self, instance, key, default=None):
        result = self.state_caches[instance].get(key)
        if result is None:
            return default
        return result

    # STREAMS:

    def create_stream(self, stream, query_entities=[]):
        self.ignite.create_cache({prop_codes.PROP_NAME: stream, prop_codes.PROP_QUERY_ENTITIES: query_entities})

    def remove_stream(self, stream):
        self.item_caches[instance].destroy()

    LISTENER_PERSIST_ALL = -1 << 63
    LISTENER_JUST_TRIGGER = ~(-1 << 63)

    def set_stream_listener_position(self, listener, stream, position):
        stream_listener = StreamListener(stream=stream, position=position)
        self.stream_listeners_cache.put(listener, stream_listener)

    def remove_stream_listener(self, listener):
        self.stream_listeners_cache.remove_key(listener)

    def write_stream_item(self, stream, key, item):
        put_if_absent_or_raise(self.item_caches[stream], key, item)

    def read_stream_item(self, stream, key):
        return self.item_caches[stream].get(key)

    def query_stream_sql(self, sql, sql_parameters):
        return self.ignite.sql(sql, page_size=1024, query_args=sql_parameters)

    # GRPC:

    async def wait_execution_finished(self, execution):
        await self.fabric_stub.ExecutionFinished(fabric_pb2.ExecutionFinishedRequest(execution=execution), wait_for_ready=True)

    async def wait_listener_attached(self, stream):
        await self.fabric_stub.ListenerAttached(fabric_pb2.ListenerAttachedRequest(stream=stream), wait_for_ready=True)

    async def enumerate_stream_item_keys(self, stream, start_key=-1, stride=0, local_to_data=False):
        stream_items = self.fabric_stub.StreamItems(
            fabric_pb2.StreamItemsRequest(stream=stream, startKey=start_key, stride=stride, localToData=local_to_data), wait_for_ready=True
        )
        await stream_items.initial_metadata()

        async def helper():
            async for item in stream_items:
                yield item.key

        return helper()

    def _create_executions_listener(self, key):
        (agent, instance) = key

        async def helper():
            executions = self.fabric_stub.Executions(fabric_pb2.ExecutionsRequest(agent=agent, instance=instance), wait_for_ready=True)

            async for proto in executions:
                if proto.cancelled:
                    value = self.execution_tasks.setdefault(proto.execution, FabricService.CANCELLED)
                    if value is not FabricService.CANCELLED:
                        value.cancel()
                        self.execution_tasks[proto.execution] = FabricService.CANCELLED
                else:
                    execution = FabricExecution(agent, proto.instance, proto.delegate, proto.execution)
                    await self.execution_channels[(agent, instance, proto.delegate)].put(execution)

        return asyncio.create_task(helper())

    async def get_executions_queue(self, agent, instance, delegate):
        self.task_collection.add(self.execution_listen_tasks[(agent, instance)])
        return self.execution_channels[(agent, instance, delegate)]

    async def set_execution_task(self, execution, task):
        value = self.execution_tasks.setdefault(execution, task)
        if value is CANCELLED:
            task.cancel()
        elif value is not task:
            warnings.warn(f"Multiple tasks set for execution '{execution}'", UserWarning)

    # UTILS:

    def write_stream_item_realtime(self, stream, item):
        self.write_stream_item(stream, self.get_current_ticks(), item)

    def write_execution_exception(self, execution, exception):
        return self.write_execution_error(execution, str(exception))

    def read_execution_result(self, execution):
        (error, result) = self.read_execution_error_and_result(execution)

        if error is not None:
            raise Exception(f"Execution failed with error: {error}")

        return result

    async def wait_execution(self, agent, instance, delegate):
        queue = await self.get_executions_queue(agent, instance, delegate)
        item = await queue.get()
        queue.task_done()
        return item

    async def enumerate_executions(self, agent, instance, delegate):
        queue = await self.get_executions_queue(agent, instance, delegate)
        while True:
            item = await queue.get()
            yield item
            queue.task_done()
