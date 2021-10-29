from pyignite.utils import entity_id
from enum import Enum

StreamDelegateTypeId = entity_id("StreamDelegateType")


class StreamDelegateType(tuple, Enum):
    function = (StreamDelegateTypeId, 0)
    action = (StreamDelegateTypeId, 1)
    external = (StreamDelegateTypeId, 2)
