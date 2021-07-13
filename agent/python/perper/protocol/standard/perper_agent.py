from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String


class PerperAgent(
    metaclass=GenericObjectMeta,
    type_name="PerperAgent",
    schema=OrderedDict(
        [
            ("Agent", String),
            ("Instance", String)
        ]
    ),
):
    pass
