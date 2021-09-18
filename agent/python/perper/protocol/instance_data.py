from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String

class InstanceData(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('agent', String),
])):
    pass

def create_instance_data(agent):
    return InstanceData(
        agent=agent,
    )
