class NotificationService:
    def __init__(self, ignite, grpc_channel, agent):
        self.agent = agent
        self.notifications_cache = ignite.get_or_create_cache('{agent}-$notifications')
