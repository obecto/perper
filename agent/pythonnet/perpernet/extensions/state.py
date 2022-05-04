from ..bindings import task_to_future, with_async_locals

from System import Object, Func
from Perper.Extensions import PerperState


async def get_state(key, default=None, default_factory=None):
    if default_factory:
        return await task_to_future(with_async_locals(lambda _: PerperState.GetOrNewAsync[Object](key, Func[Object](default_factory))))
    elif default:
        return await task_to_future(with_async_locals(lambda _: PerperState.GetOrDefaultAsync[Object](key, default)))
    else:
        return await task_to_future(with_async_locals(lambda _: PerperState.GetOrNewAsync[Object](key)))


async def set_state(key, value):
    await task_to_future(with_async_locals(lambda _: PerperState.SetAsync[Object](key, value)))


async def remove_state(key):
    await task_to_future(with_async_locals(lambda _: PerperState.RemoveAsync(key)))
