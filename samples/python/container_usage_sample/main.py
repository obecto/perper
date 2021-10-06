import asyncio
from pyignite.datatypes.primitive_objects import IntObject
from pyignite.datatypes.standard import String
from perper.model.context import *
from perper.model.agent import *
from perper.model.bootstrap import initialize


async def main():
    print("Starting container-sample #1")
    (agent1, _) = await start_agent("container-sample", [])
    print("Started container-sample #1")

    print("Starting container-sample #2")
    (agent2, _) = await start_agent("container-sample", [])
    print("Started container-sample #2")

    id1 = await agent1.call_function("Test", [1])
    id2 = await agent2.call_function("Test", [1])

    for i in range(127):
        if ((i ^ (i << 2)) & 8) == 0:
            r1 = await agent1.call_function("Test", [1])
            if r1 != id1:
                raise Exception(f"Expected to receive {id1} from agent 1, got {r1}")
        else:
            r2 = await agent2.call_function("Test", [1])
            if r2 != id2:
                raise Exception(f"Expected to receive {id2} from agent 2, got {r2}")

    print("Test passed!")

    agent1.destroy()
    agent2.destroy()

    print("Both agents destroyed!")


asyncio.run(initialize("container-usage-sample", {"Init": main}))
