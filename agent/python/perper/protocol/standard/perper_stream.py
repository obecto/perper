from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, MapObject, BoolObject


class PerperStream(
    metaclass=GenericObjectMeta,
    type_name="PerperStream",
    schema=OrderedDict(
        [
            ("stream", String),
            ("filter", MapObject),
            ("replay", BoolObject),
            ("localtodata", BoolObject)
        ]
    ),
):
    pass
