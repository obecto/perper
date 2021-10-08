from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String


class CallResultNotification(
    metaclass=GenericObjectMeta,
    type_name="CallResultNotification",
    schema=OrderedDict(
        [
            ("call", String),
            ("caller", String),
        ]
    ),
):
    pass
