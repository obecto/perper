import asyncio
from .context_vars import fabric_execution, fabric_service
from ..model import PerperAgent

startup_function_name = "Startup"


def get_agent():
    return PerperAgent(fabric_execution.get().agent, fabric_execution.get().instance)


def call(delegate, *parameters):
    return call_agent(get_agent(), delegate, *parameters)


async def start_agent(agent, *parameters):
    instance = fabric_service.get().generate_name(agent)
    fabric_service.get().create_instance(instance, agent)
    model = PerperAgent(agent, instance)
    result = await call_agent(model, startup_function_name, *parameters)
    return (model, result)


async def call_agent(agent, delegate, *parameters):
    execution = fabric_service.get().generate_name(delegate)

    try:
        fabric_service.get().create_execution(execution, agent.agent, agent.instance, delegate, parameters)
        await fabric_service.get().wait_execution_finished(execution)
        result = fabric_service.get().read_execution_result(execution)
    finally:
        fabric_service.get().remove_execution(execution)

    if result is None:
        return None
    elif len(result) == 1:
        return result[0]
    else:
        return tuple(result)


def destroy_agent(agent):
    fabric_service.get().remove_instance(agent.instance)
