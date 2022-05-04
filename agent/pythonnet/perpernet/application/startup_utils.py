from collections.abc import AsyncIterable, Awaitable

from ..bindings import future_to_task, task_to_future, with_store_async_locals, with_async_locals

from Perper.Extensions import AsyncLocals
from System import Func, Object
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus, ValueTask


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
    if "return" in func.__annotations__:
        return_type = func.__annotations__["return"]

    async def handler():
        arguments = await task_to_future(with_async_locals(lambda _: AsyncLocals.FabricService.ReadExecutionParameters(AsyncLocals.Execution)))
        result = func(*arguments)

        if isinstance(result, Awaitable):
            result = await result

        if result is None:
            await task_to_future(with_async_locals(lambda _: AsyncLocals.FabricService.WriteExecutionFinished(AsyncLocals.Execution)))

        elif isinstance(result, AsyncIterable):
            await task_to_future(with_async_locals(lambda ct: AsyncLocals.FabricService.WaitListenerAttached(AsyncLocals.Execution, cancellationToken=ct)))

            async for data in result:
                if isinstance(data, tuple) and len(data) == 2 and isinstance(data[0], int):
                    (key, data) = data
                    await task_to_future(with_async_locals(lambda _: AsyncLocals.FabricService.WriteStreamItem[return_type](AsyncLocals.Execution, key, data)))

                else:
                    # Generic Overloads are not supported by pythonnet, therefore the CurrentTicks arg
                    await task_to_future(
                        with_async_locals(
                            lambda _: AsyncLocals.FabricService.WriteStreamItem[return_type](
                                AsyncLocals.Execution, AsyncLocals.FabricService.CurrentTicks, data
                            )
                        )
                    )
            await task_to_future(with_async_locals(lambda _: AsyncLocals.FabricService.WriteExecutionFinished(AsyncLocals.Execution)))

        elif isinstance(result, tuple):
            await task_to_future(with_async_locals(lambda _: AsyncLocals.FabricService.WriteExecutionResult(AsyncLocals.Execution, result)))

        else:
            await task_to_future(with_async_locals(lambda _: AsyncLocals.FabricService.WriteExecutionResult(AsyncLocals.Execution, [result])))

    return Func[Task](lambda: future_to_task(with_store_async_locals(handler), loop))


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

    async def handler():
        result = init_func()
        if isinstance(result, Awaitable):
            result = await result
        return result

    return Func[Task](lambda: future_to_task(with_store_async_locals(handler), loop))
