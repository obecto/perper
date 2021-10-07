from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes.complex import ObjectArrayObject
from pyignite.datatypes import String, IntObject, BoolObject, MapObject, EnumObject, CollectionObject

class StreamData(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('instance', String),
    ('agent', String),
    ('delegate', String),
    ('delegateType', EnumObject),
    ('parameters', ObjectArrayObject),

    ('ephemeral', BoolObject),
    ('indexType', String),
    ('indexFields', MapObject),

    ('listeners', CollectionObject),
])):
    pass

class StreamListener(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('callerAgent', String),
    ('callerInstance', String),
    ('caller', String),
    ('parameter', IntObject),
    ('filter', MapObject),
    ('replay', BoolObject),
    ('localToData', BoolObject),
])):
    pass
