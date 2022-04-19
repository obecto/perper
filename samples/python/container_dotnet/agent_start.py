import asyncio
import uuid
from pyignite.datatypes.primitive_objects import IntObject
from perpernet import establish_connection, configure_instance, task_to_future, convert_async_iterable
from System import String, Array, Object
import clr


async def main():
    agent = "container-sample"
    fabric_service = await establish_connection()
    _, instance = configure_instance()

    startup_execution = await task_to_future(lambda ct: fabric_service.GetExecutionsReader(agent, instance, "Startup").
                                             ReadAsync(ct))
    startup_parameters = await task_to_future(
        lambda _: fabric_service.ReadExecutionParameters(startup_execution.Execution))
    await task_to_future(lambda _: fabric_service.WriteExecutionFinished(startup_execution.Execution))

    r = str(uuid.uuid4())

    async for test_execution in convert_async_iterable(
                    fabric_service.GetExecutionsReader(agent, instance, "Test").ReadAllAsync().GetAsyncEnumerator()):
        await task_to_future(lambda _: fabric_service.WriteExecutionResult(test_execution.Execution, [r]))


asyncio.run(main())
