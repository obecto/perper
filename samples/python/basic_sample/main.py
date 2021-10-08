import asyncio
import random
from pyignite.datatypes.primitive_objects import IntObject
from pyignite.datatypes.standard import String
from perper import (
    call_action,
    call_function,
    create_stream_function,
    create_stream_action,
    initialize,
    stream_enumerate,
)


async def generator(count):
    for i in range(count):
        await asyncio.sleep(0.1)
        yield f"{i}. Message"


async def processor(generator, batch_size):
    batch = []
    async for message in stream_enumerate(generator):
        batch += [message + "_processed"]
        if len(batch) == batch_size:
            yield batch
            batch = []


async def consumer(processor):
    async for batch in stream_enumerate(processor):
        print(f"Received batch of {len(batch)} messages.")
        print(", ".join(batch))


def do_something(message):
    print("DoSomething called:", message)


async def do_something_async(message):
    print("DoSomethingAsync called:", message)


def get_random_number(a, b):
    return (random.randint(a, b), IntObject)


async def get_random_number_async(a, b):
    return random.randint(a, b)


def get_two_random_numbers(a, b):
    return (random.randint(a, b), random.randint(a, b))


def count_params(a, *args):
    return a + len(args)


async def main():
    message_count = 28
    batch_count = 10

    generator = create_stream_function("Generator", [message_count])
    processor = create_stream_function("Processor", [generator, batch_count])
    _ = create_stream_action("Consumer", [processor])

    randomNumber1 = await call_function("GetRandomNumber", [1, 100])
    print(f"Random number: {randomNumber1}")

    randomNumber2 = await call_function("GetRandomNumberAsync", [1, 100])
    print(f"Random number: {randomNumber2}")

    randomNumber3, randomNumber4 = await call_function("GetTwoRandomNumbers", [1, 100])
    print(f"Random numbers: {randomNumber3} + {randomNumber4}")

    countParams = await call_function("CountParams", [1, "a", "b", "c"])
    print(f"Count params: {countParams}")

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
            "GetTwoRandomNumbers": get_two_random_numbers,
            "GetRandomNumberAsync": get_random_number_async,
            "CountParams": count_params,
        },
        {"Generator": generator, "Processor": processor, "Consumer": consumer},
    )
)
