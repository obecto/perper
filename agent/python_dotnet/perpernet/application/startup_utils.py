import asyncio
from collections.abc import AsyncIterable, Awaitable

from Perper.Application import PerperConnection, PerperStartup
from Perper.Application import PerperStartupHandlerUtils as HandlerUtils
from Perper.Extensions import AsyncLocals
from Perper.Protocol import TaskCollection
from System import Func, Action
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus

from ..extensions.context_vars import fabric_service, fabric_execution


def wrapper_delegate(func, loop):
    """Wraps a python function in an asyncio future, which in turn is wrapped in a C# Task.
    Returns a C# Func<Task>, returning the aforementioned Task, to be further handled by PerperFabric

        Parameters
        ----------
        func : function
            The function we want to embed
        loop :
            The asyncio loop to execute the funcion in (via an asyncio future)

        Returns
        -------
        Func<Task>
        """
    async def wrap():
        fabric_execution.get()
        arguments = await task_to_future(fabric_service.get().ReadExecutionParameters(AsyncLocals.Execution))
        result = func(*arguments)
    return Func[Task](lambda: future_to_task(wrap(), loop))


def wrapper_init(func, loop):
    """Wraps the python function to be used as Init function by perper in an asyncio future,
       which in turn is wrapped in a C# Task. Returns a C# Func<Task>, returning the aforementioned Task,
       to be further handled by PerperFabric.

        Parameters
        ----------
        func : function
            The perper init function
        loop :
            The asyncio loop to execute the funcion in (via an asyncio future)

        Returns
        -------
        Func<Task>
        """
    async def wrap():
        result = func()
        if isinstance(result, Awaitable):
            result = await result
    return Func[Task](lambda: future_to_task(wrap(), loop))


def task_to_future(task):
    loop = asyncio.get_running_loop()
    future = loop.create_future()

    def cont(task):
        if task.Status == TaskStatus.RanToCompletion:
            loop.call_soon_threadsafe(future.set_result, task.Result)
        elif task.Status == TaskStatus.Canceled:
            loop.call_soon_threadsafe(future.cancel)
        else:
            loop.call_soon_threadsafe(future.set_exception, task.Exception)
    task.ContinueWith(Action[Task](cont))
    return future


def future_to_task(future, loop):
    task_c = TaskCompletionSource()

    def f(fut, fabricService):
        fabric_service.set(fabricService)
        fut = asyncio.ensure_future(fut)
        fut.add_done_callback(lambda fut: task_c.SetResult())
    loop.call_soon_threadsafe(f, future, AsyncLocals.FabricService)
    return task_c.Task

