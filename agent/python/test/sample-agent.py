import asyncio
from pyignite.datatypes.primitive_objects import IntObject
from pyignite.datatypes.standard import String
from perper.model.context import *
from perper.model.agent import *
from perper.model.bootstrap import initialize


async def main():
    print("Starting simple-container-agent #1")
    (agent1, _) = await start_agent("simple-container-agent", [])
    print("Started simple-container-agent #1")

    print("Starting simple-container-agent #2")
    (agent2, _) = await start_agent("simple-container-agent", [])
    print("Started simple-container-agent #2")

    id1 = await agent1.call_function("Test", [1])
    id2 = await agent2.call_function("Test", [1])

    for i in range(127):
        if ((i ^ (i << 2)) & 8) == 0:
            r1 = await agent1.call_function("Test", [1])
            if r1 != id1:
                print("!!!", r1, id1, r1 == id2)
        else:
            r2 = await agent2.call_function("Test", [1])
            if r2 != id2:
                print("!!!", r2, id2, r2 == id1)

    print("Test passed!")

    agent1.destroy()
    agent2.destroy()

    print("Both agents destroyed!")


asyncio.run(initialize("simple-agent", {"Init": main}))
