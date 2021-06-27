def optimistic_update(cache, key, update_func):
    existing_value = cache.get(key)
    new_value = update_func(existing_value)
    cache.put(key, new_value)

    # TODO: FIX
    # while True:
    #     existing_value = cache.get(key)
    #     new_value = update_func(existing_value)
    #     if cache.replace_if_equals(key, existing_value, new_value):
    #         break

def put_if_absent_or_raise(cache, key, value):
    result = cache.put_if_absent(key, value)
    if result is None:
        raise Exception('Duplicate cache item key! (key is {key})')
