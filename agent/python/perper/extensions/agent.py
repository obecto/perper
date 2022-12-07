import asyncio

from grpc2_model_pb2 import PerperInstance
from .context_vars import fabric_execution, fabric_service

startup_function_name = "Start"
stop_function_name = "Stop"
fallback_startup_function_name = "Startup"


def get_agent_instance() -> PerperInstance:
    return PerperInstance(fabric_execution.get().agent, fabric_execution.get().instance)


def call(delegate, *parameters):
    return call_agent(get_agent_instance(), delegate, *parameters)


async def start_agent(agent, *parameters):
    
    model = await fabric_service.get().create_instance(agent)
    result = await call_agent(model, startup_function_name, *parameters)

    if result is None:
        result = await call_agent(model, fallback_startup_function_name, *parameters)

    return (model, result)


async def call_agent(instance: PerperInstance, delegate, *parameters):
    execution = None
    try:
        execution = await fabric_service.get().create_execution(instance, delegate, parameters)
        result = await fabric_service.get().read_execution_result(execution)
    finally:
        if execution is not None:
            await fabric_service.get().remove_execution(execution)

    if result is None:
        return None
    elif len(result) == 1:
        return result[0]
    else:
        return tuple(result)


async def destroy_agent(instance: PerperInstance):
    await call_agent(instance, stop_function_name)
    await fabric_service.get().remove_instance(instance)
