def optimistic_update(cache, key, update_func):
    # while True:
    #     existing_value = cache.get(key)
    #     new_value = update_func(existing_value)
    #     if cache.replace_if_equals(key, existing_value, new_value):
    #         break

    existing_value = cache.get(key)
    #print(existing_value)
    new_value = update_func(existing_value)
    #print(new_value)
    cache.replace(key, new_value)

def put_if_absent_or_raise(cache, key, value):
    result = cache.put_if_absent(key, value)
    if result is None:
        raise Exception('Duplicate cache item key! (key is {key})')
