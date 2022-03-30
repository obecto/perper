import traceback
from pyignite import Client
from pyignite.utils import is_hinted
from pyignite.exceptions import ReconnectError
import grpc
from ..extensions.context_vars import fabric_service, fabric_execution
from ..protocol import FabricService, FabricExecution
from .connection import establish_connection, configure_instance
from .context import StartupContext, startup_context, run_init_delegate, register_delegate
import asyncio

from clr_loader import get_coreclr
from pythonnet import set_runtime
import sys

sys.path.append("/home/nikola/Projects/Hatchery/perper/samples/dotnet/BasicSample/bin/Debug/net5.0/")
runtime_path = "/home/nikola/Projects/Hatchery/perper/samples/dotnet/BasicSample/bin/Debug/net5.0/BasicSample.runtimeconfig.json"
rt = get_coreclr(runtime_path)
set_runtime(rt)
import clr
clr.AddReference("Perper")
from Perper.Application import PerperConnection, PerperStartup
from Perper.Extensions import AsyncLocals
from Perper.Protocol import TaskCollection
from System import Func, Action
from System.Threading.Tasks import Task


def run_notebook(*, agent="notebook", instance="notebook-Main"):  # It is important that this function is not async, as it sets context
    fabric_service.set(establish_connection())
    startup_context.set(StartupContext(agent, instance, TaskCollection()))
    fabric_execution.set(FabricExecution(agent, instance, "Main", fabric_service.get().generate_name(agent)))


async def run(agent, delegates={}, *, use_instances=False):
    perper_startup = PerperStartup
    if "Init" in delegates:
        perper_startup.AddInitHandler.Overloads[str, Func[Task]](agent, Func[Task](wrapper(delegates.pop("Init"))))

    for (delegate, function) in delegates.items():
        perper_startup.AddHandler.Overloads[str, str, Func[Task]](delegate, Task[Func](wrapper(function)))

    perper_startup.RunAsync()

        # instance = configure_instance() if use_instances else None
        #
        # task_collection = TaskCollection()
        # task_collection.Add(fabric_service.get().TaskCollection.wait(False))
        # startup_context.set(StartupContext(agent, instance, task_collection))

    # except Exception as e:
    #     print(e)
    #
    #
    # finally:
    #     try:
    #         task_collection.cancel()
    #         await fabric_service_instance.stop()
    #     except Exception as ex:
    #         print(-1)
    #         raise

def wrapper(func):
    async def wrap():
        arguments = await task_to_future(AsyncLocals.FabricService.ReadExecutionParameters(AsyncLocals.Execution))
        result = func(*arguments)
    return Func[Task](lambda: future_to_task(wrap()))

async def task_to_future(task):
    loop = asyncio.get_running_loop()
    future = loop.create_future()
    def cont(task):
        loop.call_soon_threadsafe(future.set_result, task.Status)
    task.ContinueWith(Func[Task](cont))
    await future

def future_to_task(future):
    def wait_future():
        loop = future.get_loop()
        loop.run_until_complete(future)
    task = Task.Factory.StartNew(Action(wait_future))
    return task
