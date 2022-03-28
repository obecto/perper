from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, LongObject, BoolObject


class PerperStream(
    metaclass=GenericObjectMeta,
    schema=OrderedDict([("stream", String), ("startIndex", LongObject), ("stride", LongObject), ("localToData", BoolObject)]),
):
    pass
