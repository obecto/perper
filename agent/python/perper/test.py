from pyignite import Client, GenericObjectMeta
from collections import OrderedDict
from pyignite.datatypes import String, BoolObject

from thin_client import PerperIgniteClient
from call_data import create_call_data, set_call_data_result
from stream_data import create_stream_data, create_stream_listener, stream_data_add_listener, stream_data_remove_listener
from cache_service import CacheService

ignite = PerperIgniteClient()
with ignite.connect('127.0.0.1', 10800):
    cache_service = CacheService(ignite)

    # print('Stream tests:')
    # STREAM_NAME = 'test_stream'
    # cache_service.stream_create(STREAM_NAME, 'test_instance', 'test_agent', 'test_bool_stream_delegate', 1, True, BoolObject, ephemeral = False)
    # key = cache_service.stream_write_item(STREAM_NAME, 'Hello world')
    # item = cache_service.stream_read_item(STREAM_NAME, key)
    # print(key, item)

    # listener = cache_service.stream_add_listener(STREAM_NAME, 'caller_agent', 'caller', 1)
    # print(listener)

    # cache_service.stream_remove_listener(STREAM_NAME, listener)

    print('Call tests:')
    cache_service.call_create('test_call1', 'test_instance', 'test_agent', 'test_bool_stream_delegate', 'caller_agent', 'caller', True, BoolObject)
    cache_service.call_create('test_call2', 'test_instance', 'test_agent', 'test_bool_stream_delegate', 'caller_agent', 'caller', True, BoolObject)
    
    cache_service.call_write_error('test_call1', 'Radi said it is an error!')
    error = cache_service.call_read_error('test_call1')
    print(error)

    cache_service.call_write_result('test_call2', 'Result here!', String)
    error, result = cache_service.call_read_error_and_result('test_call2')
    print(error, result)
    
