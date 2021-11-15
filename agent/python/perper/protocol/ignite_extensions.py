def optimistic_update(cache, key, update_func):
    # while True:
    #     existing_value = cache.get(key)
    #     new_value = update_func(existing_value)
    #     if cache.replace_if_equals(key, existing_value, new_value):
    #         break

    existing_value = cache.get(key)
    # print(existing_value)
    new_value = update_func(existing_value)
    # print(new_value)
    cache.replace(key, new_value)


def put_if_absent_or_raise(cache, key, value):
    result = cache.put_if_absent(key, value)
    if result is None:
        raise KeyError("Duplicate cache item key! (key is {key})")


def create_query_entity(
    key_type_name=None,
    value_type_name=None,
    query_fields=[],
    query_indexes=[],
    table_name=None,
    key_field_name=None,
    value_field_name=None,
    field_name_aliases=[],
):
    return {
        "table_name": table_name,
        "key_field_name": key_field_name,
        "key_type_name": key_type_name,
        "field_name_aliases": field_name_aliases,
        "query_fields": query_fields,
        "query_indexes": query_indexes,
        "value_type_name": value_type_name,
        "value_field_name": value_field_name,
    }


def create_query_field(name, type_name, *, is_key_field=False, is_notnull_constraint_field=None, default_value=None, precision=-1, scale=-1):
    return dict(
        name=name,
        type_name=type_name,
        is_key_field=is_key_field,
        is_notnull_constraint_field=is_notnull_constraint_field,
        default_value=default_value,
        precision=precision,
        scale=scale,
    )


def create_query_alias(field_name, alias):
    return dict(field_name=field_name, alias=alias)


def create_query_index(index_name, index_type, inline_size, fields=[]):
    return dict(index_name=index_name, index_type=index_type, inline_size=inline_size, fields=fields)


def create_query_index_field(name, is_descending=False):
    return dict(name=name, is_descending=is_descending)
