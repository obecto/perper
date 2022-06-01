import asyncio
from collections import namedtuple
from contextvars import ContextVar

from ..bindings import restore_async_locals, task_to_future
from ..bindings.context_vars import fabric_service, fabric_execution
from .startup_utils import create_delegate_handler, create_init_handler

from System import Func
from System.Threading import CancellationToken
from System.Threading.Tasks import Task
from Perper.Application import PerperConnection, PerperStartup, PerperStartupHandlerUtils
from Perper.Protocol import FabricExecution, TaskCollection


StartupContext = namedtuple("StartupContext", ["agent", "instance", "task_collection"])
startup_context = ContextVar("startup_context")


def run_notebook(*, agent="notebook", instance="notebook-Main"):
    fabric_service.set(PerperConnection.EstablishConnection().Result)
    startup_context.set(StartupContext(agent, instance, TaskCollection()))
    fabric_execution.set(FabricExecution(agent, instance, "Init", f"{instance}-init", getattr(CancellationToken, "None")))


def register_delegate(delegate, function):
    restore_async_locals()
    handler = create_delegate_handler(function, asyncio.get_event_loop())
    PerperStartup.ListenExecutions(
        startup_context.get().task_collection,
        startup_context.get().agent,
        startup_context.get().instance,
        delegate,
        handler,
        getattr(CancellationToken, "None"),
    )


async def run(agent, delegates=None, *, use_instances=False, use_deploy_init=False):
    if delegates is None:
        delegates = {}

    loop = asyncio.get_event_loop()
    perper_startup = PerperStartup()

    if use_instances:
        perper_startup = perper_startup.WithInstances()

    if use_deploy_init:
        perper_startup = perper_startup.WithDeployInit()

    if "Init" in delegates:
        init_func = delegates.pop("Init")
        perper_startup = perper_startup.AddInitHandler.Overloads[str, Func[Task]](
            agent, PerperStartupHandlerUtils.WrapHandler(create_init_handler(init_func, loop))
        )

    for (delegate, function) in delegates.items():
        perper_startup = perper_startup.AddHandler.Overloads[str, str, Func[Task]](
            agent, delegate, PerperStartupHandlerUtils.WrapHandler(create_delegate_handler(function, loop))
        )

    await task_to_future(lambda ct: perper_startup.RunAsync(ct))
