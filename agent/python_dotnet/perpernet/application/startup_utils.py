from collections.abc import AsyncIterable, Awaitable

from Perper.Extensions import AsyncLocals
from System import Func, Object
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus, ValueTask

from .async_utils import future_to_task, task_to_future
from ..extensions.context_vars import fabric_service, fabric_execution


def create_delegate_handler(func, loop):
    """Creates a handler for a delegate perper function to be further handled by PerperFabric.
    Returns a C# Func<Task>, which corresponds to the func input

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
    if 'return' in func.__annotations__:  # TODO: Works only with basic types and tuples, expand
        return_type = func.__annotations__['return']

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
    """Creates a handler for the python function to be used as Init function by perper.
    Result is to be further handled by PerperFabric

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


