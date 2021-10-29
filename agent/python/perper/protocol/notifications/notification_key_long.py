from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import LongObject, String


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
