from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import LongObject, String, ByteArray
from pyignite.datatypes.base import IgniteDataType


# class NotificationKey(metaclass = GenericObjectMeta, type_name="NotificationKey", schema = OrderedDict([
#     ('key', LongObject)
# ])):


class NotificationKeyString(metaclass = GenericObjectMeta, type_name="NotificationKeyString", schema = OrderedDict([
    ('key', LongObject),
    ('affinity', String)
])):
    pass


class NotificationKeyLong(metaclass = GenericObjectMeta, type_name="NotificationKeyLong", schema = OrderedDict([
    ('key', LongObject),
    ('affinity', LongObject)
])):
    pass