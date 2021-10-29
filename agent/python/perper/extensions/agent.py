import asyncio
from .context_vars import fabric_execution, fabric_service
from ..model import PerperAgent

startup_function_name = "Startup"


def get_agent():
    return PerperAgent(fabric_execution.get().agent, fabric_execution.get().instance)


def call_function(delegate, *parameters):
    return agent_call_function(get_agent(), delegate, *parameters)


def call_action(delegate, *parameters):
    return agent_call_action(get_agent(), delegate, *parameters)


async def start_agent(agent, *parameters):
    instance = fabric_service.get().generate_name(agent)
    fabric_service.get().create_instance(instance, agent)
    model = PerperAgent(agent, instance)
    result = await agent_call_function(model, startup_function_name, *parameters)
    return (model, result)


async def agent_call_function(agent, delegate, *parameters):
    result = await _agent_call(agent, delegate, parameters)

    if result is None:
        return None
    elif len(result) == 1:
        return result[0]
    else:
        return tuple(result)


async def agent_call_action(agent, delegate, *parameters):
    await _agent_call(agent, delegate, parameters)


async def _agent_call(agent, delegate, parameters):
    execution = fabric_service.get().generate_name(delegate)

    try:
        fabric_service.get().create_execution(execution, agent.agent, agent.instance, delegate, parameters)
        await fabric_service.get().wait_execution_finished(execution)
        result = fabric_service.get().read_execution_result(execution)
    finally:
        fabric_service.get().remove_execution(execution)

    return result


def destroy_agent(agent):
    fabric_service.get().remove_instance(agent.instance)
