from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, IntObject, BoolObject, MapObject


class StreamListener(
    metaclass=GenericObjectMeta,
    type_name="StreamListener",
    schema=OrderedDict(
        [
            ("agentdelegate", String),
            ("stream", String),
            ("parameter", IntObject),
            ("filter", MapObject),
            ("replay", BoolObject),
            ("localtodata", BoolObject),
        ]
    ),
):
    pass
