import asyncio

from System import Action
from System.Threading.Tasks import TaskCompletionSource


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

    def f(fut):
        fut = asyncio.ensure_future(fut)
        if not return_type:
            fut.add_done_callback(lambda _future: task_c.SetResult())
        else:
            fut.add_done_callback(lambda _future: task_c.SetResult(_future.result()))

    loop.call_soon_threadsafe(f, future)
    return task_c.Task
