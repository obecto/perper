import asyncio
import time
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
        data = SimpleData(name='radi', priority=1, json='{ "id" : ' + str(x + 1) + ', "price": 1234 }')
        print(data)
        streams_cache.put(x, data)
        await asyncio.sleep(2)

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

async def execute():
    # BLANK GENERATOR EXAMPLE
    stream = context.create_blank_stream(basename='generator')
    stream.get_enumerable({}, False, False).add_listener()
    context.stream_action("blank_generator", {0: stream.stream_name, 1: 10}, None)

# asyncio.run(execute())
# asyncio.run(perper.functions(functions))

# Python 3.6
loop = asyncio.get_event_loop()
loop.run_until_complete(execute())
loop.run_until_complete(perper.functions(functions))
