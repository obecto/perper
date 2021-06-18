from pyignite import Client
from pyignite.datatypes import BinaryObject
from pyignite.utils import int_overflow
import ctypes

class PerperIgniteClient(Client):
    @Client.compact_footer.setter
    def compact_footer(self, val):
        pass

    def optimistic_update(self, cache, key, update_func):
        while True:
            existing_value = cache.get(key)
            new_value = update_func(existing_value)
            if (cache.replace(key, existing_value, new_value)): # TODO: Fix cache.replace usage
                break

    def put_if_absent_or_raise(self, cache, key, value):
        result = cache.put_if_absent(key, value)
        if result is None:
            raise Exception('Duplicate cache item key! (key is {key})')

