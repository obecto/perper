from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, BoolObject, ObjectArrayObject


class ExecutionData(
    metaclass=GenericObjectMeta,
    schema=OrderedDict(
        [
            ("agent", String),
            ("instance", String),
            ("delegate", String),
            ("finished", BoolObject),
            ("parameters", ObjectArrayObject),
            ("result", ObjectArrayObject),
            ("error", String),
        ]
    ),
):
    pass
