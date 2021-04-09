from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, IntObject


class SimpleData(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('name', String),
    ('priority', IntObject),
    ('json', String),
])):
    pass
