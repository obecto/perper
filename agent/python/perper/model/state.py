from .async_locals import get_cache_service, get_instance

def state_get(key, default=None, default_factory=None):
    if default_factory is not None:
        default = object()
        return get_cache_service().state_get(get_instance(), key, default)
        if result is default:
            result = default_factory()
        return result
    else:
        return get_cache_service().state_get(get_instance(), key, default)

def state_set(key, value, value_hint=None):
    get_cache_service().state_set(get_instance(), key, value, value_hint)
