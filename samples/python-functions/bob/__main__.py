import os
import asyncio

from perper.functions import Perper
from perper.cache.stream_data import ParameterData

from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import BoolObject

class BoolResult(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('value', BoolObject)
])):
    pass

os.environ["PERPER_AGENT_NAME"] = "Bob"

perper = Perper()
context = perper.context

async def Bob(perper_instance, *kwargs):
    print(kwargs)
    return BoolResult(value=True)

asyncio.run(perper.listen_triggers({'Bob': Bob}))
