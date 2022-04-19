from System import Object, Func
from System.Threading.Tasks import Task
import System
from Perper.Extensions import AsyncLocals, PerperState

from ..application import task_to_future
from .context_vars import fabric_execution, fabric_service


async def get_state(key, default=None, default_factory=None):
    AsyncLocals.SetExecution(fabric_execution.get())
    AsyncLocals.SetConnection(fabric_service.get())

    if default_factory:
        return await task_to_future(lambda _: PerperState.GetOrNewAsync[Object](key, Func[Object](default_factory)))
    elif default:
        return await task_to_future(lambda _: PerperState.GetOrDefaultAsync[Object](key, default))
    else:
        return await task_to_future(lambda _: PerperState.GetOrNewAsync[Object](key))


async def set_state(key, value):
    AsyncLocals.SetExecution(fabric_execution.get())
    AsyncLocals.SetConnection(fabric_service.get())
    await task_to_future(lambda _: PerperState.SetAsync[Object](key, value))


def remove_state(key):
    fabric_service.get().remove_state_value(fabric_execution.get().instance, key)
