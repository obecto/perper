from .agent import Agent
from .stream import Stream
from .async_locals import *
from .stream_flags import StreamFlags
from perper.protocol import StreamDelegateType
from perper.protocol.standard import PerperAgent
from perper.protocol.standard import PerperStream

startup_function_name = 'Startup'

async def start_agent(agent, parameters):
    instance = get_cache_service().generate_name(agent)
    get_cache_service().instance_create(instance, agent)
    model = Agent(PerperAgent(agent, instance))
    result = await model.call_function(startup_function_name, parameters)
    return (model, result)

def stream_function(delegate, parameters, flags=StreamFlags.default, index=('', {})):
    stream = get_cache_service().generate_name(delegate)
    create_stream(stream, delegate, StreamDelegateType.function, parameters, flags, index)
    return Stream(PerperStream(stream))

def stream_action(delegate, parameters, flags=StreamFlags.default, index=('', {})):
    stream = get_cache_service().generate_name(delegate)
    create_stream(stream, delegate, StreamDelegateType.action, parameters, flags, index)
    return Stream(PerperStream(stream))

def create_blank_stream(flags=StreamFlags.default, index=('', {})):
    stream = get_cache_service().generate_name("")
    create_stream(stream, "", StreamDelegateType.external, flags, index)
    return Stream(PerperStream(stream))

def create_stream(stream, delegate, delegate_type, parameters, flags, index):
    ephemeral = bool(flags & StreamFlags.ephemeral)
    index_type = None
    index_fields = None

    if (flags & StreamFlags.query):
        index_type = index[0]
        index_fields = index[1]

    get_cache_service().stream_create(stream, get_local_agent(), get_instance(), delegate, delegate_type, parameters, ephemeral, index_type, index_fields)
