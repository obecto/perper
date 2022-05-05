import traceback
from pyignite import Client
from pyignite.utils import is_hinted
from pyignite.exceptions import ReconnectError
import grpc
from ..extensions.context_vars import fabric_service, fabric_execution
from ..protocol import FabricService, FabricExecution, TaskCollection
from .connection import establish_connection, configure_instance
from .context import StartupContext, startup_context, run_init_delegate, register_delegate


def run_notebook(*, agent="notebook", instance="notebook-Main"):  # It is important that this function is not async, as it sets context
    fabric_service.set(establish_connection())
    startup_context.set(StartupContext(agent, instance, TaskCollection()))
    fabric_execution.set(FabricExecution(agent, instance, "Main", fabric_service.get().generate_name(agent)))


async def run(agent, delegates={}, *, use_instances=False, use_deploy_init=False):
    fabric_service_instance = establish_connection()
    fabric_service.set(fabric_service_instance)
    try:
        instance = configure_instance() if use_instances else None

        task_collection = TaskCollection()
        task_collection.add(fabric_service.get().task_collection.wait(False))
        startup_context.set(StartupContext(agent, instance, task_collection))

        if "Init" in delegates:
            run_init_delegate(delegates.pop("Init"), use_deploy_init)

        # The `Start` and `Startup` checks here are needed only until we remove support for the deprecated `Startup`
        if "Start" not in delegates:
            delegates["Start"] = lambda *args: fabric_service.get().write_execution_finished(fabric_execution.get().execution)

        if "Startup" not in delegates:
            delegates["Startup"] = lambda *args: fabric_service.get().write_execution_finished(fabric_execution.get().execution)

        if "Stop" not in delegates:
            delegates["Stop"] = lambda *args: fabric_service.get().write_execution_finished(fabric_execution.get().execution)

        for (delegate, function) in delegates.items():
            register_delegate(delegate, function)

        await task_collection.wait()
    finally:
        try:
            task_collection.cancel()
            await fabric_service_instance.stop()
        except Exception as ex:
            print(-1)
            raise
