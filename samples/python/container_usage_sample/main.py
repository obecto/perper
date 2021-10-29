import asyncio
import perper


async def init():
    print("Starting container-sample #1")
    (agent1, _) = await perper.start_agent("container-sample")
    print("Started container-sample #1")

    print("Starting container-sample #2")
    (agent2, _) = await perper.start_agent("container-sample")
    print("Started container-sample #2")

    id1 = await perper.call_agent(agent1, "Test", 1)
    id2 = await perper.call_agent(agent2, "Test", 1)

    for i in range(127):
        if ((i ^ (i << 2)) & 8) == 0:
            r1 = await perper.call_agent(agent1, "Test", 1)
            if r1 != id1:
                raise Exception(f"Expected to receive {id1} from agent 1, got {r1}")
        else:
            r2 = await perper.call_agent(agent2, "Test", 1)
            if r2 != id2:
                raise Exception(f"Expected to receive {id2} from agent 2, got {r2}")

    print("Test passed!")

    perper.destroy_agent(agent1)
    perper.destroy_agent(agent2)

    print("Both agents destroyed!")


asyncio.run(perper.run("container-usage-sample", {"Init": init}))
