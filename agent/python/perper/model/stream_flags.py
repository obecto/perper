from enum import Flag


class StreamFlags(Flag):
    ephemeral = 1
    default = ephemeral
    query = 2
    none = 0
