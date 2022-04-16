import asyncio
from collections import namedtuple
from contextvars import ContextVar

from System.Threading import CancellationToken
from Perper.Application import PerperStartup
from Perper.Extensions import AsyncLocals
from .startup_utils import create_delegate_handler
from ..extensions.context_vars import fabric_service, fabric_execution

StartupContext = namedtuple("StartupContext", ["agent", "instance", "task_collection"])
startup_context = ContextVar("startup_context")


def register_delegate(delegate, function):
    AsyncLocals.SetConnection(fabric_service.get())
    AsyncLocals.SetExecution(fabric_execution.get())
    loop = asyncio.get_event_loop()
    handler = create_delegate_handler(function, loop)
    PerperStartup.ListenExecutions(startup_context.get().task_collection, startup_context.get().agent,
                                   startup_context.get().instance, delegate, handler, getattr(CancellationToken, 'None'))
