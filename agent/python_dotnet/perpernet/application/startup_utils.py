import asyncio
from collections.abc import AsyncIterable, Awaitable

from Perper.Extensions import AsyncLocals
from System import Func, Action, Tuple
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus

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

    async def wrap(_execution):
        AsyncLocals.SetExecution(_execution)
        arguments = await task_to_future(fabric_service.get().ReadExecutionParameters(AsyncLocals.Execution))
        result = func(*arguments)

        if isinstance(result, Awaitable):
            result = await result
        if result is None:
            await task_to_future(fabric_service.get().WriteExecutionFinished(AsyncLocals.Execution))
        elif isinstance(result, AsyncIterable):
            await task_to_future(fabric_service.get().WaitListenerAttached(AsyncLocals.Execution))
            async for data in result:
                if isinstance(data, tuple) and len(data) == 2 and isinstance(data[0], int):
                    (key, data) = data
                    await task_to_future(fabric_service.get().WriteStreamItem(AsyncLocals.Execution, key, data))
                else:
                    await task_to_future(fabric_service.get().WriteStreamItem(AsyncLocals.Execution, data))
            await task_to_future(fabric_service.get().WriteExecutionFinished(AsyncLocals.Execution))
        # elif isinstance(result, tuple) and not is_hinted(result):
        #     await task_to_future(fabric_service.get().WriteExecutionResult(AsyncLocals.Execution, list(result)))
        else:
            await task_to_future(fabric_service.get().WriteExecutionResult(AsyncLocals.Execution, [result]))

        return result

    if "return" in func.__annotations__:  # TODO: Works only with basic types, expand
        return_type = func.__annotations__["return"]
        return Func[Task](lambda: future_to_task(wrap(AsyncLocals.FabricExecution), loop,
                                                 return_type=return_type))
    else:
        return Func[Task](lambda: future_to_task(wrap(AsyncLocals.FabricExecution), loop))


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

    async def wrap(_execution):
        AsyncLocals.SetExecution(_execution)
        result = init_func()
        if isinstance(result, Awaitable):
            result = await result
        return result

    return Func[Task](lambda: future_to_task(wrap(AsyncLocals.FabricExecution), loop))


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


def future_to_task(future, loop, return_type=None):
    parsed = False
    _return_type = return_type
    if hasattr(return_type, '__origin__'):
        #  We are dealing with an annotation class from the typing library
        _return_type = parse_tuple(return_type) if return_type.__origin__ == tuple else return_type
        parsed = True

    task_c = TaskCompletionSource() if not _return_type else TaskCompletionSource[_return_type]()

    def f(fut, _fabric_service):
        fabric_service.set(_fabric_service)
        fut = asyncio.ensure_future(fut)
        if not return_type:
            fut.add_done_callback(lambda _future: task_c.SetResult())
        else:
            if parsed:
                fut.add_done_callback(lambda _future: task_c.SetResult(parse_tuple(return_type, _future.result())))
            else:
                fut.add_done_callback(lambda _future: task_c.SetResult(_future.result()))

    loop.call_soon_threadsafe(f, future, AsyncLocals.FabricService)
    return task_c.Task


def parse_tuple(tuple_type, instance=None):
    tuple_subtypes = tuple_type.__args__
    t = Tuple[tuple_subtypes]
    return t(*instance) if instance else clr.GetClrType(t)

