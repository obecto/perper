import asyncio
import random
from pyignite.datatypes.primitive_objects import IntObject
from pyignite.datatypes.standard import String
from perper.model.context import *
from perper.model.agent import *
from perper.model.stream import *
from perper.model.bootstrap import initialize


async def generator(count):
    for i in range(count):
        await asyncio.sleep(0.1)
        yield f"{i}. Message"


async def processor(generator, batch_size):
    batch = []
    async for message in Stream(generator).enumerate():
        batch += [message + "_processed"]
        if len(batch) == batch_size:
            yield batch
            batch = []


async def consumer(processor):
    async for batch in Stream(processor).enumerate():
        print(f"Received batch of {len(batch)} messages.")
        print(", ".join(batch))


def do_something(message):
    print("DoSomething called:", message)


async def do_something_async(message):
    print("DoSomethingAsync called:", message)


def get_random_number(a, b):
    return (random.randint(a, b), IntObject)


async def get_random_number_async(a, b):
    return (random.randint(a, b), IntObject)


async def main(*args):
    message_count = 28
    batch_count = 10

    generator = stream_function("Generator", [message_count])
    processor = stream_function("Processor", [generator.raw_stream, batch_count])
    _ = stream_action("Consumer", [processor.raw_stream])

    randomNumber = await call_function("GetRandomNumber", [1, 100])
    print(f"Random number: {randomNumber}")

    anotherRandomNumber = await call_function("GetRandomNumberAsync", [1, 100])
    print(f"Random number: {anotherRandomNumber}")

    await call_action("DoSomething", ["123"])
    await call_action("DoSomethingAsync", ["456"])


asyncio.run(
    initialize(
        "basic-sample",
        {
            "Init": main,
            "DoSomething": do_something,
            "DoSomethingAsync": do_something_async,
            "GetRandomNumber": get_random_number,
            "GetRandomNumberAsync": get_random_number_async,
        },
        {"Generator": generator, "Processor": processor, "Consumer": consumer},
    )
)
