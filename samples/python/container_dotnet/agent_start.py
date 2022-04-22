import asyncio
import uuid
from pyignite.datatypes.primitive_objects import IntObject
from perpernet import establish_connection, configure_instance, task_to_future, convert_async_iterable
from System import String, Array, Object
from System.Collections import Hashtable
import clr
import argparse

parser = argparse.ArgumentParser(description="Description for my parser")
parser.add_argument("-n", "--Name", help="Name: The name of the agent", required=False, default="container-sample")
argument = parser.parse_args()


async def main(agent):
    fabric_service = await establish_connection()
    _, instance = configure_instance()

    startup_execution = await task_to_future(lambda ct: fabric_service.GetExecutionsReader(agent, instance, "Startup").
                                             ReadAsync(ct))
    startup_parameters = await task_to_future(
        lambda _: fabric_service.ReadExecutionParameters(startup_execution.Execution))
    print("Startup parameters ", tuple(startup_parameters))
    dict = Hashtable()
    dict.Add("f", "w")
    await task_to_future(lambda _: fabric_service.WriteExecutionResult(startup_execution.Execution, [dict]))

    r = str(uuid.uuid4())

    async for test_execution in convert_async_iterable(
                    fabric_service.GetExecutionsReader(agent, instance, "Test").ReadAllAsync().GetAsyncEnumerator()):
        await task_to_future(lambda _: fabric_service.WriteExecutionResult(test_execution.Execution, [r]))


asyncio.run(main(argument.Name))
