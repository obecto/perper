from datetime import datetime
import uuid
import logging

from pyignite.datatypes import CollectionObject
from pyignite.utils import hashcode, entity_id
from perper.utils import PerperThinClient

from perper.services import PerperTypeUtils
from perper.services import Serializer
from perper.cache import CallData
from perper.cache import StreamData, ParameterData
from perper.cache import StreamDelegateType
from perper.model import StreamFlags
from perper.model import Stream
from perper.model import Agent


class Context:
    def __init__(
        self,
        instance,
        fabric,
        state,
        ignite=PerperThinClient(),
        logger=logging.getLogger("context"),
    ):
        self.ignite = ignite
        self.instance = instance
        self.serializer = Serializer()
        self.fabric = fabric
        self.state = state
        self._logger = logger
        self.agent = Agent(
            agent_name=self.instance.agent,
            agent_delegate=self.fabric.agent_delegate,
            context=self,
            serializer=self.serializer,
        )

    async def start_agent(self, delegate_name, parameters):
        agent_delegate = delegate_name
        call_delegate = delegate_name

        agent_name = self.generate_name(agent_delegate)
        agent = Agent(
            agent_name=agent_name,
            agent_delegate=agent_delegate,
            context=self,
            serializer=self.serializer,
        )

        result = await agent.call_function(call_delegate, parameters)

        return (agent, result)

    def stream_function(self, function_name, parameters, flags):
        stream_name = self.generate_name(function_name)
        self.create_stream(
            stream_name,
            StreamDelegateType.function,
            function_name,
            parameters,
            None,
            flags,
        )
        # return Stream(stream_name, self.fabric, self.ignite, instance = self.instance, serializer = self.serializer, state = self.state)
        return Stream(streamname=stream_name)

    def stream_action(self, action_name, parameters, flags):
        stream_name = self.generate_name(action_name)
        self.create_stream(
            stream_name, StreamDelegateType.action, action_name, parameters, None, flags
        )
        # return Stream(stream_name, self.fabric, self.ignite, instance = self.instance, serializer = self.serializer, state = self.state)
        return Stream(streamname=stream_name)

    def declare_stream_function(self, function_name):
        stream_name = self.generate_name(function_name)
        stream = Stream(
            stream_name,
            self.fabric,
            self.ignite,
            instance=self.instance,
            serializer=self.serializer,
            state=self.state,
        )
        stream.function_name = function_name
        return stream

    def initialize_stream_function(self, stream, parameters, flags):
        stream_instance = stream
        if stream_instance.function_name == None:
            raise Exception("Stream is already initialized")

        self.create_stream(
            stream_instance.stream_name,
            StreamDelegateType.function,
            stream_instance.function_name,
            parameters,
            None,
            flags,
        )

    def create_blank_stream(self, flags=StreamFlags.default):
        stream_name = self.generate_name()
        self.create_stream(
            stream_name, StreamDelegateType.external, "", {}, None, flags
        )

    def create_stream(
        self, stream_name, delegate_type, delegate_name, parameters, type_, flags
    ):
        streams_cache = self.ignite.get_cache("streams")
        index_type = None
        stream_data = StreamData(
            agent=self.instance.agent,
            agentdelegate=self.fabric.agent_delegate,
            delegate=delegate_name,
            delegatetype=(entity_id("StreamDelegateType"), delegate_type.value),
            # TODO: Parameters should be passed as BinaryObject
            parameters=ParameterData(parameters=(1, parameters)),
            listeners=(1, []),
            # Perper type utils is not implemented
            indextype=(PerperTypeUtils.get_java_type_name(type_) or type_.name)
            if ((flags and StreamFlags.query) != 0 and type_ != None)
            else None,
            indexfields=(
                1,
                {},
            ),  # self.serializer.get_queriable_fields(type) if (flags & self.stream_flags.query) != 0 and type != None else None,
            ephemeral=(flags and StreamFlags.ephemeral) != 0,
        )
        streams_cache.put(stream_name, stream_data)
        self._logger.debug(f"Created stream with name :{stream_name}")

    async def call(self, agent_name, agent_delegate, call_delegate, parameters):
        calls_cache = self.ignite.get_cache("calls")
        call_name = self.generate_name(call_delegate)
        call_data = CallData(
            agent=agent_name,
            agentdelegate=agent_delegate,
            delegate=call_delegate,
            calleragentdelegate=self.fabric.agent_delegate
            if self.fabric.agent_delegate != None
            else "",
            caller=self.instance.instance_name,
            finished=False,
            localtodata=True,
            error="",
            parameters=ParameterData(parameters=(1, parameters)),
        )
        calls_cache.put(call_name, call_data)

        self._logger.debug(f"Created call with name: {call_name}")
        key, notification = await self.fabric.get_call_notification(call_name)
        self.fabric.consume_notification(key)

        call = calls_cache.get_and_remove(notification.call)

        if call.error != "":
            raise Exception(f"Exception in call to {call_delegate}: {call.error}")
        else:
            return call

    def generate_name(self, basename=None):
        return f"{basename}-{uuid.uuid4()}"
