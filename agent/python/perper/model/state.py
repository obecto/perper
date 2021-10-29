from .async_locals import *

def state_get(key, default=None):
    return get_cache_service().state_get(get_instance(), key, default)

def state_get_or_new(key, default_factory):
    BLANK = object()
    result == state_get(key, BLANK)
    if result is BLANK:
        result = default_factory()
    return result

def state_set(key, value, value_hint=None):
    get_cache_service().state_set(get_instance(), key, value, value_hint)
