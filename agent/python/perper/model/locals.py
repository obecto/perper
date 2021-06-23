class Locals:
    cache_service = None
    notification_service = None

    @staticmethod
    def set_connection(cache_service, notification_service):
        Locals.cache_service = cache_service
        Locals.notification_service = notification_service
