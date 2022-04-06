import asyncio
from .context_vars import fabric_execution, fabric_service
# from ..model import PerperAgent
from ..application.startup_utils import task_to_future

startup_function_name = "Startup"
from Perper.Extensions import PerperContext, AsyncLocals
from System import Action, Array, Object, Type
import clr

#
# def get_agent():
#     return PerperAgent(fabric_execution.get().agent, fabric_execution.get().instance)


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


# async def start_agent(agent, *parameters):
#     instance = fabric_service.get().generate_name(agent)
#     fabric_service.get().create_instance(instance, agent)
#     model = PerperAgent(agent, instance)
#     result = await call_agent(model, startup_function_name, *parameters)
#     return (model, result)


# async def call_agent(agent, delegate, *parameters):
#     execution = fabric_service.get().generate_name(delegate)
#
#     try:
#         fabric_service.get().create_execution(execution, agent.agent, agent.instance, delegate, parameters)
#         await fabric_service.get().wait_execution_finished(execution)
#         result = fabric_service.get().read_execution_result(execution)
#     finally:
#         fabric_service.get().remove_execution(execution)
#
#     if result is None:
#         return None
#     elif len(result) == 1:
#         return result[0]
#     else:
#         return tuple(result)

#
# def destroy_agent(agent):
#     fabric_service.get().remove_instance(agent.instance)
