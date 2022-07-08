import traceback
import backoff
from collections import namedtuple
from collections.abc import AsyncIterable, Awaitable
from pyignite import Client
from pyignite.utils import is_hinted
from pyignite.exceptions import ReconnectError
from ..extensions.context_vars import fabric_service, fabric_execution
from ..protocol import FabricService, FabricExecution, TaskCollection
from contextvars import ContextVar

StartupContext = namedtuple("StartupContext", ["agent", "instance", "task_collection"])
startup_context = ContextVar("startup_context")


def run_init_delegate(function, use_deploy_init=False):
    if use_deploy_init:
        init_instance = startup_context.get().instance if startup_context.get().instance is not None else f"{startup_context.get().agent}-Init"
        init_execution = FabricExecution(startup_context.get().agent, init_instance, "Init", f"{init_instance}-Init")
        startup_context.get().task_collection.add(process_execution(init_execution, function, is_init=True))
    else:
        register_delegate("Init", function, True)


def register_delegate(delegate, function, is_init=False):
    async def helper():
        if is_init:
            async for execution in fabric_service.get().enumerate_executions("Registry", startup_context.get().agent, "Run"):
                if startup_context.get().instance is None or startup_context.get().instance == execution.execution:
                    transformed_execution = FabricExecution(startup_context.get().agent, execution.execution, "Init", f"{execution.execution}-init")
                    startup_context.get().task_collection.add(process_execution(transformed_execution, function, is_init))
        else:
            async for execution in fabric_service.get().enumerate_executions(startup_context.get().agent, startup_context.get().instance, delegate):
                startup_context.get().task_collection.add(process_execution(execution, function))

    startup_context.get().task_collection.add(helper())


async def process_execution(execution, function, is_init=False):
    try:
        fabric_execution.set(execution)

        parameters = [] if is_init else fabric_service.get().read_execution_parameters(fabric_execution.get().execution)
        result = function(*parameters)

        if isinstance(result, Awaitable):
            result = await result

        if not is_init:
            if result is None:
                fabric_service.get().write_execution_finished(fabric_execution.get().execution)
            elif isinstance(result, AsyncIterable):
                # NOTE: unlike C# code, here we read the parameters before waiting for a listener.
                await fabric_service.get().wait_listener_attached(fabric_execution.get().execution)
                async for data in result:
                    if isinstance(data, tuple) and len(data) == 2 and isinstance(data[0], int):
                        (key, data) = data
                        fabric_service.get().write_stream_item(fabric_execution.get().execution, key, data)
                    else:
                        fabric_service.get().write_stream_item_realtime(fabric_execution.get().execution, data)
                fabric_service.get().write_execution_finished(fabric_execution.get().execution)
            elif isinstance(result, tuple) and not is_hinted(result):
                fabric_service.get().write_execution_result(fabric_execution.get().execution, list(result))
            else:
                fabric_service.get().write_execution_result(fabric_execution.get().execution, [result])
    except Exception as ex:
        print(f"Error while executing {fabric_execution.get().execution}:")
        traceback.print_exception(type(ex), ex, ex.__traceback__)
        if not is_init:
            try:
                fabric_service.get().write_execution_exception(fabric_execution.get().execution, ex)
            except Exception as ex:
                print(f"Error while executing {fabric_execution.get().execution}:")
                traceback.print_exception(type(ex), ex, ex.__traceback__)
