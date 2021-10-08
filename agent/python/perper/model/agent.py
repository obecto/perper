import asyncio
from .async_locals import get_cache_service, get_notification_service, get_local_agent, get_instance, get_execution
from perper.protocol.standard import PerperAgent

startup_function_name = 'Startup'

def get_agent():
    return PerperAgent(get_local_agent(), get_instance())

def call_function(delegate, parameters):
    return agent_call_function(get_agent(), delegate, parameters)

def call_action(delegate, parameters):
    return agent_call_action(get_agent(), delegate, parameters)

async def start_agent(agent, parameters):
    instance = get_cache_service().generate_name(agent)
    get_cache_service().instance_create(instance, agent)
    model = PerperAgent(agent, instance)
    result = await agent_call_function(model, startup_function_name, parameters)
    return (model, result)

async def agent_call_function(agent, delegate, parameters):
    result = await __agent_call(agent, delegate, parameters, get_cache_service().call_read_result)

    if result is None:
        return None
    elif len(result) == 1:
        return result[0]
    else:
        return tuple(result)

async def agent_call_action(agent, delegate, parameters):
    await __agent_call(agent, delegate, parameters, get_cache_service().call_check_result)

async def __agent_call(agent, delegate, parameters, result_handler):
    call = get_cache_service().generate_name(delegate)

    call_notification_task = asyncio.create_task(get_notification_service().get_call_result_notification(call))  # HACK: Workaround bug in fabric
    get_cache_service().call_create(call, agent.agent, agent.instance, delegate, get_local_agent(), get_instance(), parameters)

    (k, n) = await call_notification_task
    result = result_handler(call)
    get_cache_service().call_remove(call)
    get_notification_service().consume_notification(k)

    return result

def agent_destroy(agent):
    get_cache_service().instance_destroy(agent.instance)

