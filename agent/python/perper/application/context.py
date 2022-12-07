import traceback
from typing import Callable
import backoff
from collections import namedtuple
from collections.abc import AsyncIterable, Awaitable

from grpc2_model_pb2 import PerperInstance
from ..extensions.context_vars import fabric_service, fabric_execution
from ..protocol import FabricService, FabricExecution, TaskCollection
from contextvars import ContextVar

StartupContext = namedtuple("StartupContext", ["agent", "instance", "task_collection"])
startup_context = ContextVar("startup_context")


def run_init_delegate(function: Callable, use_deploy_init=False):
    if use_deploy_init:
        init_instance: str = startup_context.get().instance if startup_context.get().instance is not None else f"{startup_context.get().agent}-Init"
        init_execution = FabricExecution(
            startup_context.get().agent,
            init_instance,
            "Init",
            f"{init_instance}-Init",
            [],
        )
        startup_context.get().task_collection.add(process_execution(init_execution, function, is_init=True))
    else:
        register_delegate("Init", function, True)


def register_delegate(delegate: str, function: Callable, is_init=False):
    async def helper():
        if is_init:
            async for execution in fabric_service.get().listen_executions(
                PerperInstance(instance=startup_context.get().agent, agent="Registry"),
                "Run",
            ):
                if startup_context.get().instance is None or startup_context.get().instance == execution.execution:
                    transformed_execution = FabricExecution(
                        startup_context.get().agent,
                        execution.execution,
                        "Init",
                        f"{execution.execution}-init",
                        [],
                    )
                    startup_context.get().task_collection.add(process_execution(transformed_execution, function, is_init))
        else:
            async for execution in fabric_service.get().listen_executions(
                PerperInstance(
                    instance=startup_context.get().instance,
                    agent=startup_context.get().agent,
                ),
                delegate,
            ):
                startup_context.get().task_collection.add(process_execution(execution, function))

    startup_context.get().task_collection.add(helper())


async def process_execution(execution: FabricExecution, function: Callable, is_init=False):
    try:
        fabric_execution.set(execution)

        result = function(*execution.arguments)

        if isinstance(result, Awaitable):
            result = await result

        if not is_init:
            if result is None:
                await fabric_service.get().write_execution_finished(fabric_execution.get().execution)
            elif isinstance(result, AsyncIterable):
                async for data in result:
                    if isinstance(data, tuple) and len(data) == 2 and isinstance(data[0], int):
                        (key, data) = data
                        await fabric_service.get().write_stream_item(fabric_execution.get().execution, key, data)
                    else:
                        await fabric_service.get().write_stream_item_realtime(fabric_execution.get().execution, data)
                await fabric_service.get().write_execution_finished(fabric_execution.get().execution)
            else:
                await fabric_service.get().write_execution_result(fabric_execution.get().execution, result)
    except Exception as ex:
        print(f"Error while executing {fabric_execution.get().execution}:")
        traceback.print_exception(type(ex), ex, ex.__traceback__)
        if not is_init:
            try:
                await fabric_service.get().write_execution_exception(fabric_execution.get().execution, ex)
            except Exception as ex:
                print(f"Error while executing {fabric_execution.get().execution}:")
                traceback.print_exception(type(ex), ex, ex.__traceback__)
