from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, LongObject, ObjectArrayObject


class StreamListener(
    metaclass=GenericObjectMeta,
    schema=OrderedDict([("stream", String), ("position", LongObject)]),
):
    pass
