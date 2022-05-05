from contextvars import ContextVar
from Perper.Extensions import AsyncLocals

fabric_service = ContextVar("fabric_service")
fabric_execution = ContextVar("fabric_execution")


def restore_async_locals():
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())


def with_async_locals(f):
    """Intended usage: task_to_future(with_async_locals(<c# sync handler>))"""

    def wrapped_f(*args, **kwargs):
        restore_async_locals()
        return f(*args, **kwargs)

    return wrapped_f


def store_async_locals():
    service = AsyncLocals.FabricService
    execution = AsyncLocals.FabricExecution

    def store():
        fabric_service.set(service)
        fabric_execution.set(execution)

    return store


def with_store_async_locals(f):
    """Intended usage: future_to_task(with_store_async_locals(<async handler>), loop)"""
    captured = store_async_locals()

    async def wrapped_f(*args, **kwargs):
        captured()
        return await f(*args, **kwargs)

    return wrapped_f()
