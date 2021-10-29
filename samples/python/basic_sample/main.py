import asyncio
import random
import perper
from pyignite.datatypes import IntObject


async def node1(other):
    yield 1
    async for number in perper.enumerate_stream(other):
        print(f"Node 1 received {number}.")
        if number > 10:
            break
        await asyncio.sleep(0.1)
        yield number - 1


async def node2(other):
    async for number in perper.enumerate_stream(other):
        print(f"Node 2 received {number}.")
        if number > 10:
            break
        await asyncio.sleep(0.1)
        yield number + 2


async def generator(count):
    for i in range(count):
        await asyncio.sleep(0.1)
        yield f"{i}. Message"


async def processor(generator, batch_size):
    batch = []
    async for message in perper.enumerate_stream(generator):
        batch += [message + "_processed"]
        if len(batch) == batch_size:
            yield batch
            batch = []


async def consumer(processor):
    async for batch in perper.enumerate_stream(processor):
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


async def init():
    # Streams
    message_count = 28
    batch_count = 10

    generator = perper.start_stream("Generator", message_count)
    processor = perper.start_stream("Processor", generator, batch_count)
    _ = perper.start_stream("Consumer", processor, action=True)

    # Cyclic streams
    (node1_stream, node1_start) = perper.declare_stream("Node1")
    (node2_stream, node2_start) = perper.declare_stream("Node2", action=True)
    node1_start(node2_stream)
    node2_start(node1_stream)

    # Calls
    randomNumber1 = await perper.call("GetRandomNumber", 1, 100)
    print(f"Random number: {randomNumber1}")

    randomNumber2 = await perper.call("GetRandomNumberAsync", 1, 100)
    print(f"Random number: {randomNumber2}")

    randomNumber3, randomNumber4 = await perper.call("GetTwoRandomNumbers", 1, 100)
    print(f"Random numbers: {randomNumber3} + {randomNumber4}")

    countParams = await perper.call("CountParams", 1, "a", "b", "c")
    print(f"Count params: {countParams}")

    await perper.call("DoSomething", "123")
    await perper.call("DoSomethingAsync", "456")


asyncio.run(
    perper.run(
        "basic-sample",
        {
            "Init": init,
            "DoSomething": do_something,
            "DoSomethingAsync": do_something_async,
            "GetRandomNumber": get_random_number,
            "GetTwoRandomNumbers": get_two_random_numbers,
            "GetRandomNumberAsync": get_random_number_async,
            "CountParams": count_params,
            "Generator": generator,
            "Processor": processor,
            "Consumer": consumer,
            "Node1": node1,
            "Node2": node2,
        },
    )
)
