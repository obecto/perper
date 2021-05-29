import sys
import time
import random
import asyncio

from perper.functions import Perper
from perper.cache.stream_data import ParameterData

from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, IntObject


class SimpleData(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('name', String),
    ('priority', IntObject),
    ('json', String),
])):
    pass


async def blank_generator(perper_instance, *kwargs):
    print('Generating...')
    streams_cache = perper_instance.ignite.get_cache(kwargs[1][0])
    for x in range(kwargs[1][1]):
        data = SimpleData(
            name='RadiTest',
            priority=1,
            json='{ "id" : ' + str(x + 1) + ', "price": ' + str(random.randrange(1000, 2000)) + ' }'
        )
        # TODO: Think of a better way for generating item keys
        streams_cache.put(random.randrange(1, sys.maxsize), data)
        
        print(data)
        await asyncio.sleep(1)

# def processor(perper_instance, *kwargs):
#     streams_cache = perper_instance.ignite.get_cache(kwargs[1][1].streamname)
#     stream_data = streams_cache.get(kwargs[1][1].streamname)
#     stream_data.parameters = ParameterData(parameters=(1, {0:kwargs[1][0]}))

# def consumer(perper_instance, *kwargs):
#     streams_cache = perper_instance.ignite.get_cache('streams')
#     result = streams_cache.get_and_remove(kwargs[1][0].streamname)
#     print("Consumed:")
#     print(result)

functions = {
    "blank_generator": blank_generator
}

perper = Perper()
context = perper.context

stream = context.create_blank_stream(basename='generator')
print(stream)

input("Press Enter to continue...")

async def execute():
    # BLANK GENERATOR EXAMPLE
    context.stream_action("blank_generator", {0: stream.stream_name, 1: 20}, None)

asyncio.run(execute())
asyncio.run(perper.functions(functions))
