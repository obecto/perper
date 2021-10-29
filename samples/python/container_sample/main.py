import asyncio
import uuid
from pyignite.datatypes.primitive_objects import IntObject
from perper.application.connection import establish_connection, configure_instance


async def main():
    agent = "container-sample"
    fabric_service = establish_connection()
    instance = configure_instance()
    startup_execution = await fabric_service.wait_execution(agent, instance, "Startup")
    fabric_service.write_execution_finished(startup_execution.execution)

    r = uuid.uuid4()

    async for test_execution in fabric_service.enumerate_executions(agent, instance, "Test"):
        fabric_service.write_execution_result(test_execution.execution, [r])


asyncio.run(main())
