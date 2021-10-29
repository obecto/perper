from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, BoolObject
from pyignite.datatypes.complex import ObjectArrayObject


class CallData(
    metaclass=GenericObjectMeta,
    schema=OrderedDict(
        [
            ("instance", String),
            ("agent", String),
            ("delegate", String),
            ("parameters", ObjectArrayObject),
            ("callerAgent", String),
            ("caller", String),
            ("localToData", BoolObject),
            ("finished", BoolObject),
            ("result", ObjectArrayObject),
            ("error", String),
        ]
    ),
):
    pass
