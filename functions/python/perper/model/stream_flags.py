from enum import Enum


class StreamFlags(Enum):
    ephemeral = 1
    default = ephemeral
    query = 2
    none = 0
