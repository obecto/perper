from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String


class StreamTriggerNotification(
    metaclass=GenericObjectMeta,
    schema=OrderedDict(
        [
            ("delegate", String),
            ("stream", String),
        ]
    ),
):
    pass
