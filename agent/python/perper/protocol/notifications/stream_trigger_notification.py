from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String


class StreamTriggerNotification(
    metaclass=GenericObjectMeta,
    type_name='StreamTriggerNotification',
    schema=OrderedDict(
        [
            ('delegate', String),
            ('instance', String),
            ('stream', String),
        ]
    ),
):
    pass
