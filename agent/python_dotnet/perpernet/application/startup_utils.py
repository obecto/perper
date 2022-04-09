import asyncio
from collections.abc import AsyncIterable, Awaitable

from Perper.Extensions import AsyncLocals
from System import Func, Action, Tuple, Boolean, Object
from System.Runtime.CompilerServices import ValueTaskAwaiter
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus, ValueTask
from System.Threading.Tasks.Sources import IValueTaskSource

from ..extensions.context_vars import fabric_service, fabric_execution
import clr


def create_delegate_handler(func, loop):
    """Wraps a python function in an asyncio future, which in turn is wrapped in a C# Task.
    Returns a C# Func<Task>, returning the aforementioned Task, to be further handled by PerperFabric

        Parameters
        ----------
        func : function
            The function we want to embed
        loop :
            The asyncio loop to execute the function in (via an asyncio future)

        Returns
        -------
        Func<Task>
        """
    return_type = Object
    if "return" in func.__annotations__:  # TODO: Works only with basic types and tuples, expand
        return_type = func.__annotations__["return"]

    async def wrap(_fabric, _execution):
        fabric_service.set(_fabric)
        fabric_execution.set(_execution)
        arguments = await task_to_future(fabric_service.get().ReadExecutionParameters(_execution.Execution))
        result = func(*arguments)

        if isinstance(result, Awaitable):
            result = await result
        if result is None:
            await task_to_future(fabric_service.get().WriteExecutionFinished(_execution.Execution))
        elif isinstance(result, AsyncIterable):
            await task_to_future(fabric_service.get().WaitListenerAttached(_execution.Execution))
            async for data in result:
                if isinstance(data, tuple) and len(data) == 2 and isinstance(data[0], int):
                    (key, data) = data
                    await task_to_future(fabric_service.get().WriteStreamItem[return_type](_execution.Execution, key, data))
                else:
                    # Generic Overloads are not supported by pythonnet, therefore the CurrentTicks arg
                    await task_to_future(fabric_service.get().WriteStreamItem[return_type](_execution.Execution,
                                         fabric_service.get().CurrentTicks, data))
            await task_to_future(fabric_service.get().WriteExecutionFinished(_execution.Execution))
        elif isinstance(result, tuple):
            await task_to_future(fabric_service.get().WriteExecutionResult(_execution.Execution, result))
        else:
            await task_to_future(fabric_service.get().WriteExecutionResult(_execution.Execution, [result]))

    return Func[Task](lambda: future_to_task(wrap(AsyncLocals.FabricService, AsyncLocals.FabricExecution), loop))


def create_init_handler(init_func, loop):
    """Wraps the python function to be used as Init function by perper in an asyncio future,
       which in turn is wrapped in a C# Task. Returns a C# Func<Task>, returning the aforementioned Task,
       to be further handled by PerperFabric.

        Parameters
        ----------
        init_func : function
            The perper init function
        loop :
            The asyncio loop to execute the function in (via an asyncio future)

        Returns
        -------
        Func<Task>
        """

    async def wrap(_fabric, _execution):
        fabric_service.set(_fabric)
        fabric_execution.set(_execution)
        result = init_func()
        if isinstance(result, Awaitable):
            result = await result
        return result

    return Func[Task](lambda: future_to_task(wrap(AsyncLocals.FabricService, AsyncLocals.FabricExecution), loop))


def task_to_future(task, value_task=False):
    if value_task:
        task = task.AsTask()
    loop = asyncio.get_running_loop()
    future = loop.create_future()

    def cont():
        try:
            loop.call_soon_threadsafe(future.set_result, task.Result)
        except Exception as e:
            loop.call_soon_threadsafe(future.set_exception, e)

    task.GetAwaiter().OnCompleted(Action(cont))
    return future


def future_to_task(future, loop, return_type=None):
    _return_type = return_type
    task_c = TaskCompletionSource() if not _return_type else TaskCompletionSource[_return_type]()

    def f(fut, _fabric_service):
        fut = asyncio.ensure_future(fut)
        if not return_type:
            fut.add_done_callback(lambda _future: task_c.SetResult())
        else:
            fut.add_done_callback(lambda _future: task_c.SetResult(_future.result()))

    loop.call_soon_threadsafe(f, future, AsyncLocals.FabricService)
    return task_c.Task
