import uuid

class CacheService:
    def __init__(self, ignite):
        self.ignite = ignite
        self.streams_cache = ignite.get_or_create_cache('streams')
        self.calls_cache = ignite.get_or_create_cache('calls')

    # Add GetCurrentTicks alternative

    def generate_name(self, basename=None):
        return f"{basename}-{uuid.uuid4()}"
