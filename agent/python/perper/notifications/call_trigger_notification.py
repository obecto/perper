from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String


class CallTriggerNotification(
    metaclass=GenericObjectMeta,
    schema=OrderedDict(
        [
            ("call", String),
            ("delegate", String),
        ]
    ),
):
    pass
