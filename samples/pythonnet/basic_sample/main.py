import asyncio
import random

from typing import Tuple

import perpernet
from System import Int32


async def node1(other):
    yield {"test": 1}
    async for dict in perpernet.enumerate_stream_with_keys(other):
        print(f"Node 1 received key: {dict.Item1}, value: {dict.Item2}.")
        if dict.Item2["test"] > 10:
            break
        await asyncio.sleep(0.1)
        yield {"test": dict.Item2["test"] - 1}


async def node2(other):
    async for dict in perpernet.enumerate_stream_with_keys(other):
        print(f"Node 2 received {dict.Item1}, {dict.Item2}.")
        if dict.Item2["test"] > 10:
            break
        await asyncio.sleep(0.1)
        yield {"test": dict.Item2["test"] + 2}


async def generator(count):
    count = count["count"]
    for i in range(count):
        await asyncio.sleep(0.1)
        yield f"{i}. Message"


async def processor(generator, batch_size):
    batch = []
    async for message in perpernet.enumerate_stream(generator):
        batch += [message + "_processed"]
        if len(batch) == batch_size:
            yield batch
            batch = []


async def consumer(processor):
    async for batch in perpernet.enumerate_stream(processor):
        print(f"Received batch of {len(batch)} messages.")
        print(", ".join(batch))


def do_something(message):
    print("DoSomething called:", message)


async def do_something_async(message):
    print("DoSomethingAsync called:", message)


def get_random_number(a, b):
    return random.randint(a, b)


async def get_random_number_async(a, b):
    return random.randint(a, b)


def get_two_random_numbers(a, b):
    return random.randint(a, b), random.randint(a, b)


def count_params(a, *args):
    return a + len(args)


async def init():
    # Streams
    # The first time goes through without Int32, seg fault second time
    message_count = {"count": 28}
    batch_count = Int32(10)

    generator = await perpernet.start_stream("Generator", message_count)
    processor = await perpernet.start_stream("Processor", generator, batch_count)
    _ = await perpernet.start_stream("Consumer", processor, action=True)

    # Cyclic streams
    (node1, node1_start) = perpernet.declare_stream("Node1")
    (node2, node2_start) = perpernet.declare_stream("Node2", action=True)
    await node1_start(node2)
    await node2_start(node1)

    # Calls
    randomNumber1 = await perpernet.call("GetRandomNumber", 1, 100, void=False)
    print(f"Random number: {randomNumber1}")

    randomNumber2 = await perpernet.call("GetRandomNumberAsync", 1, 100, void=False)
    print(f"Random number: {randomNumber2}")
    #
    (randomNumber3, randomNumber4) = await perpernet.call("GetTwoRandomNumbers", 1, 100, void=False)
    print(f"Random numbers: {randomNumber3} + {randomNumber4}")
    #
    countParams = await perpernet.call("CountParams", 1, "a", "b", "c", void=False)
    print(f"Count params: {countParams}")
    # #
    await perpernet.call("DoSomething", "123")
    await perpernet.call("DoSomethingAsync", "456")


asyncio.run(
    perpernet.run(
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
        use_deploy_init=True,
    )
)
