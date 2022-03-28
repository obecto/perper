from .context_vars import fabric_execution, fabric_service


def get_state(key, default=None, default_factory=None):
    if default_factory is not None:
        default = object()
        return fabric_service.get().get_state_value(fabric_execution.get().instance, key, default)
        if result is default:
            result = default_factory()
        return result
    else:
        return fabric_service.get().get_state_value(fabric_execution.get().instance, key, default)


def set_state(key, value, value_hint=None):
    fabric_service.get().set_state_value(fabric_execution.get().instance, key, value, value_hint)


def remove_state(key):
    fabric_service.get().remove_state_value(fabric_execution.get().instance, key)
