from grpc2_model_pb2 import PerperDictionary
from .context_vars import fabric_execution, fabric_service


async def get_state(key, default=None, default_factory=None):
    dictionary = PerperDictionary(f"{fabric_execution.get().instance}-")
    if default_factory is not None:
        return await fabric_service.get().get_state_value(dictionary, key, default_factory())
    else:
        return await fabric_service.get().get_state_value(dictionary, key, default)


async def set_state(key, value):
    dictionary = PerperDictionary(f"{fabric_execution.get().instance}-")
    await fabric_service.get().set_state_value(dictionary, key, value)


async def remove_state(key):
    dictionary = PerperDictionary(f"{fabric_execution.get().instance}-")
    await fabric_service.get().remove_state_value(dictionary, key)
