import asyncio
from perperapi import Perper

def generator(perperapi_instance, *kwargs):
    print("GENERATOR!")

def processor(perperapi_instance, *kwargs):
    print("PROCESSOR!")

def consumer(perperapi_instance, *kwargs):
    print("CONSUMER!")
    streams_cache = perperapi_instance.ignite.get_cache('streams')
    print("Consumed:")
    print(streams_cache.get_and_remove(kwargs[1][0].streamname))

functions = {
    "generator": generator,
    "processor": processor,
    "consumer": consumer
}

perper = Perper()
context = perper.context

generator_stream = context.stream_function("generator", {0: 20}, None)
processor_stream = context.stream_function("processor", {0: 21, 1: generator_stream}, None)
consumer_stream = context.stream_action("consumer", {0: processor_stream}, None)

loop = asyncio.get_event_loop()
context = loop.run_until_complete(perper.functions(functions))
