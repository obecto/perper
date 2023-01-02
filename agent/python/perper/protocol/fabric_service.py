import sys
import uuid
import attr
import time
import asyncio
import grpc
import warnings

from datetime import datetime, timedelta, timezone
from collections import defaultdict, namedtuple
from google.protobuf.any_pb2 import Any
from google.protobuf import type_pb2
from google.protobuf.wrappers_pb2 import Int32Value, StringValue, BoolValue, UInt32Value, DoubleValue, FloatValue, Int64Value, UInt64Value, BytesValue
from google.protobuf.message import Message
from google.protobuf.descriptor import Descriptor, FieldDescriptor, EnumDescriptor

from .task_collection import TaskCollection
from .proto import grpc2_executions_pb2
from .proto.grpc2_executions_pb2_grpc import FabricExecutionsStub
from .proto.grpc2_model_pb2 import (
    PerperInstance,
    PerperDictionary,
    PerperExecution,
    PerperStream,
    PerperError,
)
from .proto import grpc2_states_pb2
from .proto.grpc2_states_pb2_grpc import FabricStatesDictionaryStub, FabricStatesListStub
from .proto import grpc2_streams_pb2
from .proto.grpc2_streams_pb2_grpc import FabricStreamsStub
from .proto import grpc2_pb2
from .proto.grpc2_pb2_grpc import FabricProtobufDescriptorsStub


FabricExecution = namedtuple("Execution", ["agent", "instance", "delegate", "execution", "arguments"])


class factorydict(dict):
    def __init__(self, default_factory):
        self.default_factory = default_factory

    def __missing__(self, key):
        value = self.default_factory(key)
        self[key] = value
        return value


class FabricService:
    CANCELLED = object()
    LISTENER_PERSIST_ALL = -1 << 63
    LISTENER_JUST_TRIGGER = ~(-1 << 63)

    def __init__(self, ignite, grpc_channel):
        self.task_collection = TaskCollection()

        self.grpc_channel = grpc_channel
        self.fabric_executions_stub = FabricExecutionsStub(self.grpc_channel)
        self.fabric_dictionary_stub = FabricStatesDictionaryStub(self.grpc_channel)
        self.fabric_streams_stub = FabricStreamsStub(self.grpc_channel)
        self.fabric_descriptor_stub = FabricProtobufDescriptorsStub(self.grpc_channel)

        self.execution_channels = defaultdict(asyncio.Queue)
        self.execution_tasks = {}
        self.registered_message_descriptors = []

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
        await self.task_collection.wait()
        await self.grpc_channel.close()

    # INSTANCES:

    async def create_instance(self, agent: str) -> PerperInstance:
        # TODO: Add PerperList API and implement parent-child agent list with it in State, with respective append on Create,
        # and automatic destroying of children on Remove of parent
        init_instance = PerperInstance(instance=agent, agent="Registry")
        execution = await self.create_execution(init_instance, "Run", [])
        return PerperInstance(instance=execution.execution, agent=agent)

    async def remove_instance(self, instance: PerperInstance):
        return await self.remove_execution(PerperExecution(instance.instance))

    # EXECUTIONS:

    async def create_execution(self, instance: PerperInstance, delegate, parameters) -> PerperExecution:
        execution = PerperExecution(execution=self.generate_name(delegate))
        packed_parameters = await self.pack_to_list_of_proto_messages(parameters)
        execution_create_request = grpc2_executions_pb2.ExecutionsCreateRequest(
            execution=execution,
            instance=instance,
            delegate=delegate,
            arguments=packed_parameters,
        )
        await self.fabric_executions_stub.Create(execution_create_request, wait_for_ready=True)
        return execution

    async def remove_execution(self, execution: PerperExecution):
        execution_delete_request = grpc2_executions_pb2.ExecutionsDeleteRequest(execution=execution)
        await self.fabric_executions_stub.Delete(execution_delete_request, wait_for_ready=True)

    async def write_execution_finished(self, execution: PerperExecution):
        execution_complete_request = grpc2_executions_pb2.ExecutionsCompleteRequest(execution=execution)
        await self.fabric_executions_stub.Complete(execution_complete_request, wait_for_ready=True)

    async def write_execution_result(self, execution: PerperExecution, result):
        result = await self.pack_to_list_of_proto_messages(result)
        execution_complete_request = grpc2_executions_pb2.ExecutionsCompleteRequest(execution=execution, results=result)
        await self.fabric_executions_stub.Complete(execution_complete_request, wait_for_ready=True)

    async def write_execution_error(self, execution: PerperExecution, error: PerperError):
        execution_complete_request = grpc2_executions_pb2.ExecutionsCompleteRequest(execution=execution, error=error)
        await self.fabric_executions_stub.Complete(execution_complete_request, wait_for_ready=True)

    async def read_execution_error_and_result(self, execution: PerperExecution):
        execution_result_request = grpc2_executions_pb2.ExecutionsGetResultRequest(execution=execution)
        execution_result_response: grpc2_executions_pb2.ExecutionsGetResultResponse = await self.fabric_executions_stub.GetResult(
            execution_result_request, wait_for_ready=True
        )

        unpacked_messages = []

        for message in execution_result_response.results:
            unpacked_message = await self.unpack_proto_message(message)
            unpacked_messages.append(unpacked_message)

        return (execution_result_response.error, unpacked_messages)

    async def listen_executions(self, instance: PerperInstance, delegate, reserve=True, work_group=""):
        async def helper():
            execution_listen_request = grpc2_executions_pb2.ExecutionsListenRequest(instance_filter=instance, delegate=delegate)

            if reserve:
                execution_stream = self.fabric_executions_stub.ListenAndReserve(wait_for_ready=True)

                await execution_stream.write(
                    grpc2_executions_pb2.ExecutionsListenAndReserveRequest(
                        reserve_next=1,
                        filter=execution_listen_request,
                        workgroup=work_group,
                    )
                )

                async for proto in execution_stream:
                    yield proto
                    if not proto.deleted and proto.execution is not None and proto.execution.execution != "":
                        await execution_stream.write(grpc2_executions_pb2.ExecutionsListenAndReserveRequest(reserve_next=1))
            else:
                async for proto in self.fabric_executions_stub.Listen(execution_listen_request, wait_for_ready=True):
                    yield proto

        async for proto in helper():
            if proto.deleted:
                value = self.execution_tasks.setdefault(proto.execution.execution, FabricService.CANCELLED)
                if value is not FabricService.CANCELLED:
                    value.cancel()
                    self.execution_tasks[proto.execution.execution] = FabricService.CANCELLED
            elif proto.execution is not None and proto.execution.execution != "":
                unpacked_messages = []

                for message in proto.arguments:
                    unpacked_message = await self.unpack_proto_message(message)
                    unpacked_messages.append(unpacked_message)

                execution = FabricExecution(
                    proto.instance.agent,
                    proto.instance.instance,
                    proto.delegate,
                    proto.execution,
                    unpacked_messages,
                )
                yield execution

    # STATE:

    async def create_state(self, dictionary: PerperDictionary, options=None):
        request = grpc2_states_pb2.StatesDictionaryCreateRequest(dictionary=dictionary, cache_options=options)
        await self.fabric_dictionary_stub.Create(request, wait_for_ready=True)

    async def clear_state(self, dictionary: PerperDictionary):
        request = grpc2_states_pb2.StatesDictionaryDeleteRequest(dictionary=dictionary, keep_cache=True)
        await self.fabric_dictionary_stub.Delete(request, wait_for_ready=True)

    async def destroy_state(self, dictionary: PerperDictionary):
        request = grpc2_states_pb2.StatesDictionaryDeleteRequest(dictionary=dictionary)
        await self.fabric_dictionary_stub.Delete(request, wait_for_ready=True)

    async def set_state_value(self, dictionary: PerperDictionary, key, value) -> bool:
        request = grpc2_states_pb2.StatesDictionaryOperateRequest(dictionary=dictionary, key=key, set_new_value=True, new_value=value)
        response: grpc2_states_pb2.StatesDictionaryOperateResponse = await self.fabric_dictionary_stub.Operate(request, wait_for_ready=True)
        return response.operation_successful

    async def remove_state_value(self, dictionary: PerperDictionary, key) -> bool:
        request = grpc2_states_pb2.StatesDictionaryOperateRequest(dictionary=dictionary, key=key, set_new_value=True, new_value=None)
        response: grpc2_states_pb2.StatesDictionaryOperateResponse = await self.fabric_dictionary_stub.Operate(request, wait_for_ready=True)
        return response.operation_successful

    async def get_state_value(self, dictionary: PerperDictionary, key, default=None):
        request = grpc2_states_pb2.StatesDictionaryOperateRequest(dictionary=dictionary, key=key, get_existing_value=True)
        response: grpc2_states_pb2.StatesDictionaryOperateResponse = await self.fabric_dictionary_stub.Operate(request, wait_for_ready=True)
        result = response.previous_value
        if result is None:
            if default:
                await self.set_state_value(dictionary, key, default)
            return default
        return result

    # STREAMS:

    async def create_stream(
        self,
        stream: PerperStream,
        ephemeral: bool,
        action: bool,
        options=None,
        query_entities=[],
    ):
        request = grpc2_streams_pb2.StreamsCreateRequest(stream=stream, ephemeral=ephemeral, cache_options=options)
        await self.fabric_streams_stub.Create(request, wait_for_ready=True)

        if action:
            move_listener_request = grpc2_streams_pb2.StreamsMoveListenerRequest(
                stream=stream,
                listener_name=f"{stream}-trigger",
                reached_key=self.LISTENER_JUST_TRIGGER,
            )
            await self.fabric_streams_stub.MoveListener(move_listener_request, wait_for_ready=True)

    async def remove_stream(self, stream: PerperStream):
        request = grpc2_streams_pb2.StreamsDeleteRequest(stream=stream)
        await self.fabric_streams_stub.Delete(request, wait_for_ready=True)

    async def set_stream_listener_position(self, listener: str, stream: PerperStream, position: int):
        if not listener:
            listener = self.generate_name(stream.stream)

        move_listener_request = grpc2_streams_pb2.StreamsMoveListenerRequest(stream=stream, listener_name=listener, reached_key=position)
        await self.fabric_streams_stub.MoveListener(move_listener_request, wait_for_ready=True)

    async def remove_stream_listener(self, stream: PerperStream, listener: str):
        if not listener:
            listener = self.generate_name(stream.stream)

        move_listener_request = grpc2_streams_pb2.StreamsMoveListenerRequest(stream=stream, listener_name=listener, delete_listener=True)
        await self.fabric_streams_stub.MoveListener(move_listener_request, wait_for_ready=True)

    async def write_stream_item(self, stream: PerperStream, key, item):
        if key:
            request = grpc2_streams_pb2.StreamsWriteItemRequest(stream=stream, key=key, value=item)
        else:
            request = grpc2_streams_pb2.StreamsWriteItemRequest(stream=stream, auto_key=True, value=item)
        await self.fabric_streams_stub.WriteItem(request, wait_for_ready=True)

    async def enumerate_stream_items(
        self,
        stream: PerperStream,
        listener: str,
        start_key=-1,
        stride=0,
        local_to_data=False,
    ):
        if not listener:
            listener = self.generate_name(stream.stream)

        move_listener_request = grpc2_streams_pb2.StreamsMoveListenerRequest(stream=stream, listener_name=listener, reached_key=self.LISTENER_PERSIST_ALL)
        self.fabric_streams_stub.MoveListener(move_listener_request, wait_for_ready=True)

        listen_request = grpc2_streams_pb2.StreamsListenItemsRequest(
            stream=stream,
            listener_name=listener,
            start_from_latest=stream.start_key == -1,
            local_to_data=local_to_data,
        )
        listen_response = self.fabric_streams_stub.ListenItems(listen_request, wait_for_ready=True)
        await listen_response.initial_metadata()

        async def helper():
            async for item in listen_response:
                yield (item.key, item.value)
                await self.set_stream_listener_position(listener=listener, stream=stream, position=item.key)

        return helper()

    def query_stream_sql(self, sql, sql_parameters):
        raise NotImplementedError("Query SQL is not implemented")

    async def set_execution_task(self, execution: PerperExecution, task):
        value = self.execution_tasks.setdefault(execution.execution, task)
        if value is FabricService.CANCELLED:
            task.cancel()
        elif value is not task:
            warnings.warn(f"Multiple tasks set for execution '{execution}'", UserWarning)

    # DESCRIPTORS

    async def get_message_descriptor_type(self, type_url: str) -> type_pb2.Type:
        request = grpc2_pb2.FabricProtobufDescriptorsGetRequest(type_url=type_url)
        response: grpc2_pb2.FabricProtobufDescriptorsGetResponse = await self.fabric_descriptor_stub.Get(request, wait_for_ready=True)
        return response.type

    async def get_enum_descriptor_type(self, type_url: str) -> type_pb2.Enum:
        request = grpc2_pb2.FabricProtobufDescriptorsGetRequest(type_url=type_url)
        response: grpc2_pb2.FabricProtobufDescriptorsGetResponse = await self.fabric_descriptor_stub.Get(request, wait_for_ready=True)
        return response.enum

    async def register_message_descriptor(self, descriptor: Descriptor) -> str:
        type_url = self.get_type_url(descriptor)
        descriptor_type = type_pb2.Type(name=descriptor.full_name)
        descriptor_type.source_context.file_name = descriptor.file.name
        descriptor_type.syntax = type_pb2.SYNTAX_PROTO3 if descriptor.file.syntax == "proto3" else type_pb2.SYNTAX_PROTO2

        for oneof in descriptor.oneofs:
            # is_synthetic does not seem to exist in python implementation
            # if not oneof.is_synthetic:
            descriptor_type.oneofs.append(oneof.name)

        for field_number, field_descriptor in descriptor.fields_by_number.items():
            field = type_pb2.Field(
                name=field_descriptor.name,
                number=field_number,
                kind=field_descriptor.type,
                cardinality=field_descriptor.label,
                packed=field_descriptor.GetOptions().packed,
                default_value=str(field_descriptor.default_value) if field_descriptor.default_value is not None else None,
                json_name=field_descriptor.json_name,
                # options=field_descriptor.GetOptions() for now options are ignored as in the C# implementation
                oneof_index=field_descriptor.containing_oneof.index if field_descriptor.containing_oneof else 0,
            )

            if field_descriptor.type == FieldDescriptor.TYPE_GROUP or field_descriptor.type == FieldDescriptor.TYPE_MESSAGE:
                field.type_url = await self.register_message_descriptor(field_descriptor.message_type)
            elif field_descriptor.type == FieldDescriptor.TYPE_ENUM:
                field.type_url = await self.register_enum_descriptor(field_descriptor.enum_type)

            descriptor_type.fields.append(field)

        request = grpc2_pb2.FabricProtobufDescriptorsRegisterRequest(type_url=type_url, type=descriptor_type)
        await self.fabric_descriptor_stub.Register(request, wait_for_ready=True)
        return type_url

    async def register_enum_descriptor(self, descriptor: EnumDescriptor) -> str:
        type_url = self.get_type_url(descriptor)
        enum_type = type_pb2.Enum(name=descriptor.full_name)
        enum_type.source_context.file_name = descriptor.file.name
        enum_type.syntax = type_pb2.SYNTAX_PROTO3 if descriptor.file.syntax == "proto3" else type_pb2.SYNTAX_PROTO2

        for value_descriptor in descriptor.values:
            enum_type.enumvalue.append(type_pb2.EnumValue(name=value_descriptor.name, number=value_descriptor.number))

        request = grpc2_pb2.FabricProtobufDescriptorsRegisterRequest(type_url=type_url, enum=enum_type)
        await self.fabric_descriptor_stub.Register(request, wait_for_ready=True)
        return type_url

    # UTILS:

    async def write_stream_item_realtime(self, stream, item):
        await self.write_stream_item(stream=stream, key=self.get_current_ticks(), item=item)

    async def write_execution_exception(self, execution, exception):
        return await self.write_execution_error(execution=execution, error=PerperError(message=str(exception)))

    async def read_execution_result(self, execution):
        (error, result) = await self.read_execution_error_and_result(execution)

        if error is not None and error.message != "":
            print(error.message)
            raise Exception(f"Execution failed with error: {error}")

        return result

    async def add_message_descriptor(self, descriptor: Descriptor):
        type_url = await self.register_message_descriptor(descriptor)
        self.registered_message_descriptors.append(descriptor)
        return type_url

    async def pack_to_list_of_proto_messages(self, parameters):
        try:
            iter(parameters)
        except TypeError:
            return [await self.pack_to_protobuf_any_message(parameters)]
        else:
            if isinstance(parameters, str):
                return [await self.pack_to_protobuf_any_message(parameters)]

            packed_parameters = []
            for parameter in parameters:
                packed_param = await self.pack_to_protobuf_any_message(parameter)
                packed_parameters.append(packed_param)

            return packed_parameters

    async def pack_to_protobuf_any_message(self, input_value):
        print(input_value)

        if isinstance(input_value, str):
            input_value = StringValue(value=input_value)
        elif isinstance(input_value, int):
            # check if it's 32 or 64 bit int and pack accordingly
            # TODO: Check is super basic, consider replacing with something more robust
            if input_value < 2**31:
                input_value = Int32Value(value=input_value)
            else:
                input_value = Int64Value(value=input_value)
        elif isinstance(input_value, bool):
            input_value = BoolValue(value=input_value)
        elif isinstance(input_value, float):
            # Python float is internally represented as double, so we can just pack it as such
            input_value = DoubleValue(value=input_value)
        elif isinstance(input_value, bytes):
            input_value = BytesValue(value=input_value)
        elif isinstance(input_value, (PerperStream, PerperDictionary, PerperInstance)):
            pass
        elif isinstance(input_value, Message):
            if input_value.DESCRIPTOR not in self.registered_message_descriptors:
                try:
                    type_url = self.get_type_url(input_value.DESCRIPTOR)
                    existing = await self.get_message_descriptor_type(type_url)

                    if existing is not None:
                        self.registered_message_descriptors.append(input_value.DESCRIPTOR)
                except:
                    await self.add_message_descriptor(input_value.DESCRIPTOR)
        else:
            raise ValueError(f"Unsupported type: {type(input_value)}, please use a protobuf message or a primitive type.")

        any_message = Any()
        any_message.Pack(input_value)
        return any_message

    async def unpack_proto_message(self, message):
        any_message = Any()
        any_message.CopyFrom(message)

        if (
            any_message.TypeName() == "google.protobuf.Int32Value"
            or any_message.TypeName() == "google.protobuf.Int64Value"
            or any_message.TypeName() == "google.protobuf.UInt32Value"
            or any_message.TypeName() == "google.protobuf.UInt64Value"
            or any_message.TypeName() == "google.protobuf.StringValue"
            or any_message.TypeName() == "google.protobuf.BoolValue"
            or any_message.TypeName() == "google.protobuf.DoubleValue"
            or any_message.TypeName() == "google.protobuf.FloatValue"
            or any_message.TypeName() == "google.protobuf.BytesValue"
        ):
            # instantiate the well known message from the type name
            well_known_message = getattr(sys.modules["google.protobuf.wrappers_pb2"], any_message.TypeName().split(".")[-1])()
            any_message.Unpack(well_known_message)
            return well_known_message.value
        else:
            raise NotImplementedError(f"Unpacking of unknown message types is not implemented yet.")

    def get_type_url(self, descriptor):
        return "type.googleapis.com/" + descriptor.full_name if descriptor.file.package == "google.protobuf" else "x/" + descriptor.full_name
