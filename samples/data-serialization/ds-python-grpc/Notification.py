from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import LongObject, IntObject, String, BoolObject

class Notification(metaclass=GenericObjectMeta, schema=OrderedDict()):
    pass

class StreamItemNotification(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('cache', String),
    ('ephemeral', BoolObject),
    ('key', LongObject),
    ('parameter', IntObject),
    ('stream', String)
])):
    pass

class StreamTriggerNotification(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('delegate', String),
    ('stream', String)
])):
    pass

class CallResultNotification(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('call', String),
    ('caller', String)
])):
    pass


class CallTriggerNotification(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('call', String),
    ('delegate', String)
])):
    pass

class CallData(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('agent', String),
    ('agentDelegate', String),
    ('delegate', String),
    ('callerAgentDelegate', String),
    ('caller', String),
    ('finished', BoolObject),
    ('localToData', BoolObject),

])):
    pass