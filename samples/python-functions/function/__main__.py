import asyncio
from perperapi import Perper
from perper.cache.stream_data import ParameterData

def generator(perper_instance, *kwargs):
    print("GENERATOR!")

def processor(perper_instance, *kwargs):
    print("PROCESSOR!")
    streams_cache = perper_instance.ignite.get_cache('streams')
    stream_data = streams_cache.get(kwargs[1][1].streamname)
    stream_data.parameters = ParameterData(parameters=(1, {0:kwargs[1][0]}))
    streams_cache.put(kwargs[1][1].streamname, stream_data)

def consumer(perper_instance, *kwargs):
    print("CONSUMER!")
    streams_cache = perper_instance.ignite.get_cache('streams')
    result = streams_cache.get_and_remove(kwargs[1][0].streamname)
    print("Consumed:")
    print(result)

functions = {
    "generator": generator,
    "processor": processor,
    "consumer": consumer
}

perper = Perper()
context = perper.context

async def execute():
    generator_stream = context.stream_function("generator", {0: 20}, None)
    asyncio.sleep(1000)
    processor_stream = context.stream_function("processor", {0: 21, 1: generator_stream}, None)
    asyncio.sleep(1000)
    context.stream_action("consumer", {0: processor_stream}, None)

# asyncio.run(execute(functions))
# asyncio.run(perper.functions(functions))

# Python 3.6
loop = asyncio.get_event_loop()
loop.run_until_complete(execute())
loop.run_until_complete(perper.functions(functions))
