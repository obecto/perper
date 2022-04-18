import asyncio
from asyncio import CancelledError

from System import Action
from System.Threading import CancellationTokenSource
from System.Threading.Tasks import TaskCompletionSource


def task_to_future(task):
    """Converts a dotnet Task to python future, passes the result, errors and cancellations

        Parameters
        ----------
        task : function
            function accepting a cancellation token and passing it on to the task to be converted

        Returns
        -------
        asyncio.future

    """
    token_source = CancellationTokenSource()
    token = token_source.Token
    task = task(token)

    loop = asyncio.get_running_loop()
    future = loop.create_future()

    def cancelled_callback(fut):
        if fut.cancelled():
            token_source.Cancel()

    future.add_done_callback(cancelled_callback)

    def cont():
        try:
            loop.call_soon_threadsafe(future.set_result, task.Result)
        except Exception as e:
            loop.call_soon_threadsafe(future.set_exception, e)

    task.GetAwaiter().OnCompleted(Action(cont))
    return future


def future_to_task(future, loop, return_type=None):
    """Converts a python future to a dotnet Task, passes the result, errors and cancellations

            Parameters
            ----------
            future :
                The python future we want to convert to a dotnet Task

            loop :
                The asyncio loop we want to execute the future in

            return_type :
                The return type of the future, needed for the Task in dotnet
            Returns
            -------
            Task

        """
    _return_type = return_type
    task_c = TaskCompletionSource() if not _return_type else TaskCompletionSource[_return_type]()

    def f(fut):
        fut = asyncio.ensure_future(fut)
        if not return_type:
            fut.add_done_callback(lambda _future: task_c.SetResult())
        else:
            fut.add_done_callback(lambda _future: task_c.SetResult(_future.result()))

    loop.call_soon_threadsafe(f, future)
    return task_c.Task


async def convert_async_iterable(async_iterable):
    while True:
        if not await task_to_future(lambda _: async_iterable.MoveNextAsync().AsTask()):
            break
        yield async_iterable.Current
