from pyignite import Client
from perper.services.serializer import Serializer


class State:
    def __init__(self, instance, ignite, serializer):
        self.agent = instance.agent
        self.ignite = ignite
        self.serializer = serializer

    # Leaving this here in case we need get_value to be async
    # We cannot await None objects in cache.get and there is no TryGetAsync in the pyignite cache (as opposed to the c# ignite cache)
    def get_value(self, key, default_value_factory):
        cache = self.ignite.get_or_create_cache(self.agent)
        result = cache.get(key)
        if result == None:
            default_value = default_value_factory()
            cache.put(key, self.serializer.serialize(default_value))
            return default_value

        return self.serializer.deserialize(result)

    def set_value(self, key, value):
        cache = self.ignite.get_or_create_cache(self.agent)
        return cache.put(key, self.serializer.serialize(value))
