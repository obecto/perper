import sys
import os
import time
import random
import asyncio

from perper.functions import Perper
from perper.cache.stream_data import ParameterData

from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, IntObject

os.environ['PERPER_AGENT_NAME'] = 'jupyter_generator'

class SimpleData(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('name', String),
    ('priority', IntObject),
    ('json', String),
])):
    pass

perper = Perper()
context = perper.context

async def jupyter_generator(*args):
    print('Agent started')
    return None

async def get_stream(*args):
    return context.stream_action('generate', {1: 20}, None)

async def generate(perper_instance, stream_name, *args):
    await asyncio.sleep(2) #TODO: Fix stream trigger getting when listener is present.
    print('Generating...')
    streams_cache = perper_instance.ignite.get_cache(stream_name)
    for x in range(args[1][1]):
        data = SimpleData(
            name='RadiTest',
            priority=1,
            json='{ "id" : ' + str(x + 1) + ', "price": ' + str(random.randrange(1000, 2000)) + ' }'
        )
        # TODO: Think of a better way for generating item keys
        streams_cache.put(random.randrange(1, sys.maxsize), data)
        
        print(data)
        await asyncio.sleep(1)


functions = {
    'jupyter_generator': jupyter_generator,
    'get_stream': get_stream,
    'generate': generate
}

# context.stream_action("generate", {1: 20}, None)

asyncio.run(perper.listen_triggers(functions))