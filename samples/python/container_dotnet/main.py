import asyncio
import perpernet
from System import Int32


async def init():
    print("Starting container-sample #1")
    agent1 = await perpernet.start_agent("container-sample")
    print("Started container-sample #1")

    print("Starting container-sample #2")
    agent2 = await perpernet.start_agent("container-sample")
    print("Started container-sample #2")

    id1 = await perpernet.call_agent(agent1, "Test", Int32(3), void=False)
    print(id1)
    id2 = await perpernet.call_agent(agent2, "Test", Int32(1), void=False)

    for i in range(127):
        if ((i ^ (i << 2)) & 8) == 0:
            r1 = await perpernet.call_agent(agent1, "Test", Int32(1), void=False)
            if r1 != id1:
                raise Exception(f"Expected to receive {id1} from agent 1, got {r1}")
        else:
            r2 = await perpernet.call_agent(agent2, "Test", Int32(1), void=False)
            if r2 != id2:
                raise Exception(f"Expected to receive {id2} from agent 2, got {r2}")

    print("Test passed!")

    await perpernet.destroy_agent(agent1)
    await perpernet.destroy_agent(agent2)

    print("Both agents destroyed!")


asyncio.run(perpernet.run("container-usage-sample", {"Init": init}))
