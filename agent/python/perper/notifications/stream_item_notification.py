from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import LongObject, IntObject, String, BoolObject


class StreamItemNotification(
    metaclass=GenericObjectMeta,
    schema=OrderedDict(
        [
            ("cache", String),
            ("stream", String),
            ("parameter", IntObject),
            ("ephemeral", BoolObject),
            ("key", LongObject),
        ]
    ),
):
    pass
