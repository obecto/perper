from contextvars import ContextVar

connection = ContextVar("connection")
instance = ContextVar("instance")


def set_connection(cache_service, notification_service):
    connection.set((cache_service, notification_service))


def get_cache_service():
    return connection.get()[0]


def get_notification_service():
    return connection.get()[1]


def get_local_agent():
    return get_notification_service().agent


def get_instance():
    return instance.get()[0]


def get_execution():
    return instance.get()[1]


def enter_context(_instance, execution, callback):
    instance.set((_instance, execution))
    return callback()


def set_context(_instance, execution):
    instance.set((_instance, execution))
