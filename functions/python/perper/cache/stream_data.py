from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import *


class StreamData(
    metaclass=GenericObjectMeta,
    type_name="StreamData",
    schema=OrderedDict(
        [
            ("agent", String),
            ("agentdelegate", String),
            ("delegate", String),
            ("delegatetype", EnumObject),
            ("parameters", BinaryObject),
            ("listeners", CollectionObject),
            ("indextype", String),
            ("indexfields", MapObject),
            ("ephemeral", BoolObject),
        ]
    ),
):
    pass


class ParameterData(
    metaclass=GenericObjectMeta,
    type_name="ParameterData",
    schema=OrderedDict(
        [
            ("parameters", MapObject),
        ]
    ),
):
    pass
