from pyignite import Client
from pyignite.datatypes import *
from pyignite.utils import int_overflow
import ctypes
from pyignite.datatypes.type_codes import *
from pyignite.constants import *


# def hashcode_bytes(data):
#     # https://github.com/apache/ignite-python-thin-client/commit/e0c22ef3aef39ea8a42ddb6b4495b7bcaa479417
#     result = 1
#     for byte in data:
#         byte = ctypes.c_byte(byte).value
#         result = int_overflow(31 * result + byte)

#     return result


# def fix_binary_object_hashcode(result):
#     # Get fields length assuming HAS_SCHEMA
#     fields_end = int.from_bytes(result[20:24], "little")
#     # Extract field data assuming registered type_id
#     field_buffer = result[24:fields_end]
#     # Compute hashcode as BinaryArrayIdentityResolver does.
#     hash_code = hashcode_bytes(field_buffer)
#     # Replace header hashcode
#     return result[:8] + hash_code.to_bytes(4, "little", signed=True) + result[12:]


# original_binaryobject_from_python = BinaryObject.from_python


# def binaryobject_from_python_monkeypatch(*args, **kwargs):
#     return fix_binary_object_hashcode(
#         original_binaryobject_from_python(*args, **kwargs)
#     )


# BinaryObject.from_python = binaryobject_from_python_monkeypatch


# def monkeypatch_null(cls):
#     original_parse = cls.parse

#     def parse_monkeypatch(client, *args, **kwargs):
#         buffer = client.recv(1)
#         if buffer == TC_NULL:
#             return Null.build_c_type(), buffer
#         client.prefetch = buffer + client.prefetch
#         return original_parse(client, *args, **kwargs)

#     cls.parse = parse_monkeypatch

#     original_to_python = cls.to_python

#     def to_python_monkeypatch(ctype_object, *args, **kwargs):
#         if type(ctype_object).__name__ == Null.__name__:
#             return None
#         return original_to_python(ctype_object, *args, **kwargs)

#     cls.to_python = to_python_monkeypatch

#     original_from_python = cls.from_python

#     def from_python_monkeypatch(value, *args, **kwargs):
#         if value is None:
#             return Null.from_python()
#         return original_from_python(value, *args, **kwargs)

#     cls.from_python = from_python_monkeypatch


# #
# # monkeypatch_null(Map)
# monkeypatch_null(MapObject)
# monkeypatch_null(BinaryObject)


# def anydataobject_parse_monkeypatch(client):
#     type_code = client.recv(ctypes.sizeof(ctypes.c_byte))
#     try:
#         data_class = tc_map(type_code)
#     except KeyError:
#         raise ParseError("Unknown type code: `{}`".format(type_code))
#     client.prefetch = type_code + client.prefetch
#     return data_class.parse(client)


# def anydataobject_to_python_monkeypatch(ctype_object, *args, **kwargs):
#     type_code = ctype_object.type_code.to_bytes(
#         ctypes.sizeof(ctypes.c_byte), byteorder=PROTOCOL_BYTE_ORDER
#     )
#     data_class = tc_map(type_code)
#     return data_class.to_python(ctype_object, *args, **kwargs)


# AnyDataObject.parse = anydataobject_parse_monkeypatch
# AnyDataObject.to_python = anydataobject_to_python_monkeypatch


class PerperThinClient(Client):
    @Client.compact_footer.setter
    def compact_footer(self, val):
        pass
