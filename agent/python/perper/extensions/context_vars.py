from contextvars import ContextVar
from ..protocol.fabric_service  import FabricExecution, FabricService

fabric_service: ContextVar[FabricService] = ContextVar("fabric_service")
fabric_execution: ContextVar[FabricExecution] = ContextVar("fabric_execution")
