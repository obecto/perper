from .context_vars import fabric_execution, fabric_service
from ..application import task_to_future

startup_function_name = "Startup"
from Perper.Extensions import PerperContext, AsyncLocals
from System import Action, Array, Object, Type
import clr


async def call(delegate, *parameters, void=True):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    if void:
        return await task_to_future(PerperContext.CallAsync(delegate, *parameters))
    else:
        # Object array is passed to perper as return type to avoid conversion issues
        t = Array.CreateInstance(clr.GetClrType(Object), 1).GetType()
        result = await task_to_future(PerperContext.CallAsync[t](delegate, *parameters))

        if result is None:
            return None
        elif len(result) == 1:
            return result[0]
        else:
            return tuple(result)


async def start_agent(agent_name, *parameters):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    agent = await task_to_future(PerperContext.StartAgentAsync(agent_name, *parameters))
    return agent


async def destroy_agent(agent):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    await task_to_future(AsyncLocals.FabricService.RemoveInstance(agent.Instance))
