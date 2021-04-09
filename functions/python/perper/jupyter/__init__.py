import sys
import os
from perper.model import StreamFlags
from perper.model import State
from perper.model import Context
from perper.services import Serializer
from perper.services import FabricService
from perper.services import PerperConfig
from perper.cache import PerperInstanceData
from perper.utils import PerperThinClient

sys.path.append("..")

if "PERPER_AGENT_NAME" not in os.environ:
    os.environ["PERPER_AGENT_NAME"] = "Notebook"


def azure_handler(comm, open_msg):
    # Register handler for later messages
    @comm.on_msg
    def _recv(msg):
        global data
        data = msg["content"]["data"]  # azure function json
        # Use data to
        # 1. Invoke a python function using reflection (getattr)
        # 2. If this is the first call populate missing info for initialization of global objects
        pass

    global open_message
    open_message = "Hello World"
    comm.send(open_msg["content"]["data"])


get_ipython().kernel.comm_manager.register_target("azure_handler", azure_handler)

# # other initialization of thin client, fabric, state, context

serializer = Serializer()
ignite = PerperThinClient()
ignite.compact_footer = True
ignite.connect("localhost", 10800)

config = PerperConfig()
fabric = FabricService(ignite, config)
fabric.start()

instance = PerperInstanceData(ignite, serializer)
state = State(instance, ignite, serializer)
context = Context(instance, fabric, state, ignite)

# comms registration end

# # state begin


def get_state_value(key, default_value_factory):
    return state.get_value(key, default_value_factory)


def set_state_value(key, value):
    return state.set_value(key, value)


# state end

# context begin


def stream_function(function_name, parameters, flags=StreamFlags.default):
    return context.stream_function(function_name, parameters, flags)


def stream_action(action_name, parameters, flags=StreamFlags.default):
    return context.stream_action(action_name, parameters, flags)


def declare_stream_function(function_name):
    return context.declare_stream_function(function_name)


def initialize_stream_function(stream, parameters, flags=StreamFlags.default):
    return context.initialize_stream_function(stream, parameters, flags)


def create_blank_stream(flags=StreamFlags.default):
    return context.create_blank_stream(flags)


def start_agent(delegate_name, parameters):
    return context.start_agent(delegate_name, parameters)


async def call_function(function_name, parameters):
    return context.agent.call_function(function_name, parameters)


def call_action(action_name, parameters):
    return context.agent.call_action(action_name, parameters)


# context end
