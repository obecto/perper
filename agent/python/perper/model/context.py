from agent import Agent
from stream import Stream
from async_locals import *
from stream_flags import StreamFlags
from stream_delegate_type import StreamDelegateType
from perper.protocol.standard import PerperAgent
from perper.protocol.standard import PerperStream
from perper.protocol.stream_data import create_stream

class Context:
    def get_agent(self):
        return PerperAgent(get_agent(), get_instance())

    def start_agent(self, agent, parameters):
        instance = Agent(PerperAgent(agent, get_cache_service().generata_name(agent)))
        result = instance.call_function(agent, parameters)
        return (instance, result)
    
    def stream_function(self, delegate, parameters, flags=StreamFlags.default):
        stream = get_cache_service().generata_name(delegate)
        create_stream(stream, delegate, StreamDelegateType.function, parameters, flags)
        return Stream(PerperStream(stream))
    
    def stream_action(self, delegate, parameters, flags=StreamFlags.default):
        stream = get_cache_service().generata_name(delegate)
        create_stream(stream, delegate, StreamDelegateType.action, parameters, flags)
        return Stream(PerperStream(stream))

    def create_blank_stream(self, flags=StreamFlags.default):
        stream = get_cache_service().generata_name("")
        create_stream(stream, "", StreamDelegateType.external, None, flags)
        return Stream(PerperStream(stream))

    def create_stream(self, stream, delegate, delegate_type, parameters, flags):
        ephemeral = (flags and StreamFlags.ephemeral) != 0
        index_type = None
        index_fields = None

        # TODO: Fix this:
        if ((flags and StreamFlags.query) != 0):
            index_type = 'type'
            index_fields = {}
        
        get_cache_service().stream_create(stream, get_agent(), get_instance(), delegate, delegate_type, parameters, ephemeral, index_type, index_fields)
