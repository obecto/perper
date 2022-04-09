import asyncio
from collections.abc import AsyncIterable, Awaitable

from Perper.Application import PerperConnection, PerperStartup
from Perper.Application import PerperStartupHandlerUtils as HandlerUtils
from Perper.Extensions import AsyncLocals
from Perper.Protocol import TaskCollection, FabricExecution
from System import Func, Action
from System.Threading import CancellationToken, CancellationTokenSource
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus

from .startup_utils import *
from ..extensions.context_vars import fabric_service, fabric_execution


async def run(agent, delegates={}, *, use_instances=False):
    loop = asyncio.get_event_loop()
    perper_startup = PerperStartup()
    if "Init" in delegates:
        init_func = delegates.pop("Init")
        perper_startup = perper_startup.AddInitHandler. \
            Overloads[str, Func[Task]](agent, HandlerUtils.
                                       WrapHandler(create_init_handler(init_func, loop)))

    for (delegate, function) in delegates.items():
        perper_startup = perper_startup.AddHandler. \
            Overloads[str, str, Func[Task]](agent, delegate, HandlerUtils.
                                            WrapHandler(create_delegate_handler(function, loop)))

    await task_to_future(perper_startup.RunAsync())
