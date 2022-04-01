import clr
import asyncio
from collections.abc import AsyncIterable, Awaitable

clr.AddReference("Perper")
from Perper.Application import PerperConnection, PerperStartup
from Perper.Application import PerperStartupHandlerUtils as HandlerUtils
from Perper.Extensions import AsyncLocals
from Perper.Protocol import TaskCollection
from System import Func, Action
from System.Threading.Tasks import Task, TaskCompletionSource, TaskStatus

from .startup_utils import *
from ..extensions.context_vars import fabric_service


async def run(agent, delegates={}, *, use_instances=False):
    loop = asyncio.get_event_loop()
    perper_startup = PerperStartup()
    if "Init" in delegates:
        perper_startup = perper_startup.AddInitHandler. \
            Overloads[str, Func[Task]](agent, HandlerUtils.WrapHandler(wrapper_init(delegates.pop("Init"), loop)))

    for (delegate, function) in delegates.items():
        perper_startup = perper_startup.AddHandler. \
            Overloads[str, str, Func[Task]](agent, delegate, HandlerUtils.WrapHandler(wrapper_delegate(function, loop)))

    await task_to_future(perper_startup.RunAsync())
