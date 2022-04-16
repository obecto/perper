import asyncio
from collections.abc import AsyncIterable, Awaitable

from Perper.Application import PerperConnection, PerperStartup
from Perper.Application import PerperStartupHandlerUtils as HandlerUtils
from Perper.Protocol import FabricExecution, TaskCollection
from System import Func, Action
from System.Threading import CancellationToken
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus

from .startup_utils import *
from .context import StartupContext, startup_context
from ..extensions.context_vars import fabric_service, fabric_execution


def run_notebook(*, agent="notebook", instance="notebook-Main"):
    fabric_service.set(PerperConnection.EstablishConnection().Result)
    startup_context.set(StartupContext(agent, instance, TaskCollection()))
    fabric_execution.set(FabricExecution(agent, instance, "Init", f"{instance}-init", getattr(CancellationToken, 'None')))


async def run(agent, delegates=None, *, use_instances=False):
    if delegates is None:
        delegates = {}

    loop = asyncio.get_event_loop()
    perper_startup = PerperStartup()

    if use_instances:
        perper_startup = perper_startup.WithInstances()

    if "Init" in delegates:
        init_func = delegates.pop("Init")
        perper_startup = perper_startup.AddInitHandler. \
            Overloads[str, Func[Task]](agent, HandlerUtils.
                                       WrapHandler(create_init_handler(init_func, loop)))

    for (delegate, function) in delegates.items():
        perper_startup = perper_startup.AddHandler. \
            Overloads[str, str, Func[Task]](agent, delegate, HandlerUtils.
                                            WrapHandler(create_delegate_handler(function, loop)))

    await task_to_future(lambda ct: perper_startup.RunAsync(ct))
