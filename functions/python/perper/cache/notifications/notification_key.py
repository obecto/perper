from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import LongObject, String


class NotificationKeyString(
    metaclass=GenericObjectMeta,
    type_name="NotificationKeyString",
    schema=OrderedDict(
        [
            ("affinity", String),
            ("key", LongObject),
        ]
    ),
):
    pass


class NotificationKeyLong(
    metaclass=GenericObjectMeta,
    type_name="NotificationKeyLong",
    schema=OrderedDict(
        [
            ("affinity", LongObject),
            ("key", LongObject),
        ]
    ),
):
    pass
