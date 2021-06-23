from agent import Agent
from stream import Stream
from async_locals import AsyncLocals
from stream_flags import StreamFlags
from perper.protocol.standard import PerperAgent
from perper.protocol.standard import PerperStream
from perper.protocol.stream_data import create_stream

class Context:
    def get_agent(self):
        return PerperAgent(AsyncLocals.get_agent(), AsyncLocals.get_instance())

    def start_agent(self, agent, parameters):
        instance = Agent(PerperAgent(agent, AsyncLocals.get_cache_service().generata_name(agent)))
        result = instance.call_function(agent, parameters)
        return (instance, result)
    
    def stream_function(self, delegate, parameters, flags=StreamFlags.default):
        stream = AsyncLocals.get_cache_service().generata_name(delegate)
        create_stream(stream, delegate, 0, parameters, flags)
        return Stream(PerperStream(stream))
    
    def stream_action(self, delegate, parameters, flags=StreamFlags.default):
        stream = AsyncLocals.get_cache_service().generata_name(delegate)
        create_stream(stream, delegate, 1, parameters, flags)
        return Stream(PerperStream(stream))

    def create_blank_stream(self, flags=StreamFlags.default):
        stream = AsyncLocals.get_cache_service().generata_name("")
        create_stream(stream, "", 2, None, flags)
        return Stream(PerperStream(stream))

    def create_stream(self, stream, delegate, delegate_type, parameters, flags):
        ephemeral = (flags and StreamFlags.ephemeral) != 0
        index_type = None
        index_fields = None

        # TODO: Fix this:
        if ((flags and StreamFlags.query) != 0):
            index_type = 'type'
            index_fields = {}
        
        AsyncLocals.get_cache_service().stream_create(stream, AsyncLocals.get_agent(), AsyncLocals.get_instance(), delegate, delegate_type, parameters, ephemeral, index_type, index_fields)
