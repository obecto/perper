from contextvars import ContextVar

connection = ContextVar('connection')
instance = ContextVar('instance')

class AsyncLocals:
    @staticmethod
    def set_connection(cache_service, notification_service):
        connection.set((cache_service, notification_service))
    
    @staticmethod
    def get_cache_service():
        return connection.get()[0]

    @staticmethod
    def get_notification_service():
        return connection.get()[1]

    @staticmethod
    def get_agent():
        return AsyncLocals.get_notification_service().agent

    @staticmethod
    def get_instance():
        return instance.get()

    @staticmethod
    def enter_context(inc_instance, callback):
        instance.set(inc_instance)
        return callback()
