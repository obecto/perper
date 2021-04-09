from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import *


class CallData(
    metaclass=GenericObjectMeta,
    type_name="CallData",
    schema=OrderedDict(
        [
            ("agent", String),
            ("agentdelegate", String),
            ("delegate", String),
            ("calleragentdelegate", String),
            ("caller", String),
            ("finished", BoolObject),
            ("localtodata", BoolObject),
            ("result", BinaryObject),
            ("error", String),
            ("parameters", BinaryObject),
        ]
    ),
):
    pass
