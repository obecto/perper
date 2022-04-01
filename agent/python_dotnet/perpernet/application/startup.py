import clr
import asyncio
from collections.abc import AsyncIterable, Awaitable

clr.AddReference("Perper")
from Perper.Application import PerperConnection, PerperStartup
from Perper.Application import PerperStartupHandlerUtils as HandlerUtils
from Perper.Extensions import AsyncLocals
from Perper.Protocol import TaskCollection, FabricExecution
from System import Func, Action
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus

from .startup_utils import *
from ..extensions.context_vars import fabric_service, fabric_execution


async def run(agent, delegates={}, *, use_instances=False):
    loop = asyncio.get_event_loop()
    perper_startup = PerperStartup()
    if "Init" in delegates:
        init_func = delegates.pop("Init")
        init_instance = f"{agent}-init"
        fabric_execution.set(FabricExecution(agent, init_instance, "Init", f"{init_instance}-init", cancellationToken))
        perper_startup = perper_startup.AddInitHandler. \
            Overloads[str, Func[Task]](agent, HandlerUtils.
                                       WrapHandler(create_init_handler(init_func, loop)))

    for (delegate, function) in delegates.items():
        fabric_execution.set(FabricExecution(agent, initInstance, "Init", f"{initInstance}-init", cancellationToken))
        perper_startup = perper_startup.AddHandler. \
            Overloads[str, str, Func[Task]](agent, delegate, HandlerUtils.
                                            WrapHandler(create_delegate_handler(function, loop)))

    await task_to_future(perper_startup.RunAsync())
