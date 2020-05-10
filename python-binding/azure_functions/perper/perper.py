import json
import typing

from ._perper import AbstractPerperCache

from azure.functions import meta

from pyignite import Client, GenericObjectMeta
from pyignite.datatypes import *

class PerperCache(AbstractPerperCache):
    """A concrete implementation of PerperCache type."""

    def __init__(self, *,
                 stream_name: str) -> None:
        self.__stream_name = stream_name

    
    def get_data(self) -> list:
        client = Client()
        client.connect('localhost', 10800)
           
        streams_cache = client.get_or_create_cache(json.loads(self.__stream_name)['Stream'])
        stream = streams_cache.scan()
        return list(stream)
    
    
    def __repr__(self) -> str:
        return (
            f'<azure.PerperCache '
            f'at 0x{id(self):0x}>'
        )


class PerperStreamConverter(meta.InConverter,
                        binding='perperStream'):

    @classmethod
    def check_input_type_annotation(cls, pytype: type) -> bool:
        return True

    @classmethod
    def decode(cls, data: meta.Datum, *, trigger_metadata) -> typing.Any:
        stream_name = data.value
        return PerperCache(stream_name=stream_name)
        
