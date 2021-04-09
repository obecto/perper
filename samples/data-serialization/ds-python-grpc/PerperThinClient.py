from pyignite import Client
from pyignite.datatypes import BinaryObject
from pyignite.utils import int_overflow
import ctypes

def hashcode_bytes(data):
    # https://github.com/apache/ignite-python-thin-client/commit/e0c22ef3aef39ea8a42ddb6b4495b7bcaa479417
    result = 1
    for byte in data:
        byte = ctypes.c_byte(byte).value
        result = int_overflow(31 * result + byte)

    return result

def fix_binary_object_hashcode(result):
    # Get fields length assuming HAS_SCHEMA
    fields_end = int.from_bytes(result[20:24], "little")
    # Extract field data assuming registered type_id
    field_buffer = result[24 : fields_end]
    # Compute hashcode as BinaryArrayIdentityResolver does.
    hash_code = hashcode_bytes(field_buffer)
    # Replace header hashcode
    return result[:8] + hash_code.to_bytes(4, "little", signed=True) + result[12:]

original = BinaryObject.from_python

def from_python_monkeypatch(*args, **kwargs):
    return fix_binary_object_hashcode(original(*args, **kwargs))

BinaryObject.from_python = from_python_monkeypatch

class PerperThinClient(Client):
    @Client.compact_footer.setter
    def compact_footer(self, val):
        pass
