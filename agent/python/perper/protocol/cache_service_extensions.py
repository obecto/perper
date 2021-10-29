def perper_stream_remove_listener_caller(cache_service, perper_stream, caller, parameter):
    return cache_service.stream_remove_listener(perper_stream.stream, caller, parameter)

def perper_stream_add_listener(cache_service, perper_stream, caller_agent, caller_instance, caller, parameter):
    return cache_service.stream_add_listener(perper_stream.stream, caller_agent, caller_instance, caller, parameter, perper_stream.filter, perper_stream.replay, perper_stream.localToData)

def perper_stream_remove_listener(cache_service, perper_stream, listener):
    return cache_service.stream_remove_listener(perper_stream.stream, listener)

def stream_read_notification(cache_service, notification):
    return cache_service.stream_read_item(notification.cache, notification.key)

def call_write_exception(cache_service, call, exception):
    return cache_service.call_write_error(call, str(exception))

def call_read_result(cache_service, call):
    (error, result) = cache_service.call_read_error_and_result(call)

    if error is not None:
        raise Exception(f'Call failed with error: {error}')

    return result

def call_check_result(cache_service, call):
    error = cache_service.call_read_error(call)
    if error is not None:
        raise Exception(f'Call failed with error: {error}')

