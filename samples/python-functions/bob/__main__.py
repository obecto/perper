import os
import random
import asyncio

from perper.functions import Perper
from perper.cache.stream_data import ParameterData
from perper.model import Stream

from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, IntObject

os.environ["PERPER_AGENT_NAME"] = "Bob"

perper = Perper()
context = perper.context

class SimpleStream(Stream, 
    metaclass=GenericObjectMeta,
    type_name="PerperStream`1[[SimpleData]]",
    schema=OrderedDict([("streamname", String)])
):
    pass

perper.register_stream_class(SimpleStream)

async def Bob(perper_instance, *kwargs):
    stream = context.create_blank_stream(basename='generator')
    print(stream)
    return stream

class IntWrap(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('value', IntObject),
])):
    pass


async def get_random_number(*kwargs):
    return IntWrap(value=random.randrange(kwargs[2][0], kwargs[2][1]))

async def log_something(*kwargs):
    print(kwargs[2])

functions = {
    'Bob': Bob,
    'get_random_number': get_random_number,
    'log_something': log_something
}

asyncio.run(perper.listen_triggers(functions))
