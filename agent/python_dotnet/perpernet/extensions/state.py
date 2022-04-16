from System import Object
from Perper.Extensions import AsyncLocals, PerperState

from ..application import task_to_future
from .context_vars import fabric_execution, fabric_service


async def get_state(key, default=None, default_factory=None):
    if default:
        return await task_to_future(lambda _: PerperState.GetOrNewAsync[Object](key, default))
    else:
        return await task_to_future(lambda _: PerperState.GetOrNewAsync[Object](key))


def set_state(key, value):
    AsyncLocals.SetExecution(fabric_execution.get())
    await task_to_future(lambda _: PerperState.SetAsync[Object](key, value))


def remove_state(key):
    fabric_service.get().remove_state_value(fabric_execution.get().instance, key)
