import asyncio
from collections import namedtuple
from contextvars import ContextVar

from Perper.Application import PerperStartup
from .startup_utils import create_delegate_handler

StartupContext = namedtuple("StartupContext", ["agent", "instance", "task_collection"])
startup_context = ContextVar("startup_context")


def register_delegate(delegate, function):
    loop = asyncio.get_event_loop()
    handler = create_delegate_handler(function, loop)
    PerperStartup.ListenExecutions(startup_context.get().task_collection, startup_context.get().agent,
                                   startup_context.get().instance, delegate, handler, None)
