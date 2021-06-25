from pyignite import Client
from pyignite.datatypes import BinaryObject
from pyignite.utils import int_overflow
import ctypes

class PerperIgniteClient(Client):
    @Client.compact_footer.setter
    def compact_footer(self, val):
        pass

