from .context_vars import fabric_execution, fabric_service
from ..application import task_to_future

startup_function_name = "Startup"
from Perper.Extensions import PerperContext, AsyncLocals, PerperAgentExtensions
from System import Action, Array, Object, Type
import clr


async def call(delegate, *parameters, void=True):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    if void:
        return await task_to_future(lambda _: PerperContext.CallAsync(delegate, *parameters))
    else:
        # Object array is passed to perper as return type to avoid conversion issues
        t = Array.CreateInstance(clr.GetClrType(Object), 1).GetType()
        result = _handle_result(await task_to_future(lambda _: PerperContext.CallAsync[t](delegate, *parameters)))
        return result


async def call_agent(agent, delegate, *parameters, void=True):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    if void:
        return await task_to_future(lambda _: PerperAgentExtensions.CallAsync(agent, delegate, *parameters))
    else:
        # Object array is passed to perper as return type to avoid conversion issues
        t = Array.CreateInstance(clr.GetClrType(Object), 1).GetType()
        result = _handle_result(await task_to_future(lambda _: PerperAgentExtensions.CallAsync[t](agent, delegate, *parameters)))
        return result


def _handle_result(result):
    try:
        if result is None:
            return None
        elif len(result) == 1:
            return result[0]
        else:
            return tuple(result)

    except Exception as e:
        print("Result type cannot be handled ", e)


async def start_agent(agent_name, *parameters, void=True):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    if void:
        agent = await task_to_future(lambda _: PerperContext.StartAgentAsync(agent_name, *parameters))
        return agent
    else:
        result = await task_to_future(lambda _: PerperContext.StartAgentAsync[Object](agent_name, *parameters))
        agent = result.Item1
        result = result.Item2
        return agent, result


async def destroy_agent(agent):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    await task_to_future(lambda _: AsyncLocals.FabricService.RemoveInstance(agent.Instance))


