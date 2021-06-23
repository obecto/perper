from contextvars import ContextVar

class AsyncLocals:
    connection = ContextVar('connection')
    instance = ContextVar('instance')

    @staticmethod
    def set_connection(cache_service, notification_service):
        AsyncLocals.connection.set((cache_service, notification_service))
    
    @staticmethod
    def get_cache_service():
        return AsyncLocals.connection.get()[0]

    @staticmethod
    def get_notification_service():
        return AsyncLocals.connection.get()[1]
