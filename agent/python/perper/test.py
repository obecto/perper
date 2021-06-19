import time
import grpc

from pyignite import Client, GenericObjectMeta
from collections import OrderedDict
from pyignite.datatypes import String, BoolObject

from thin_client import PerperIgniteClient
from call_data import create_call_data, set_call_data_result
from cache_service import CacheService
from notification_service import NotificationService
from stream_data import *

import asyncio

ignite = PerperIgniteClient()
async def test():
    with ignite.connect('127.0.0.1', 10800):
        cache_service = CacheService(ignite)
        channel = grpc.insecure_channel('127.0.0.1:40400')
        notification_service = NotificationService(ignite, channel, 'caller_agent')

        # print('Stream cache tests:')
        # STREAM_NAME = 'test_stream'
        # cache_service.stream_create(STREAM_NAME, 'test_instance', 'test_agent', 'test_bool_stream_delegate', 1, True, BoolObject, ephemeral = False)
        # key = cache_service.stream_write_item(STREAM_NAME, 'Hello world')
        # item = cache_service.stream_read_item(STREAM_NAME, key)
        # print(key, item)

        # listener = cache_service.stream_add_listener(STREAM_NAME, 'caller_agent', 'caller', 1)
        # print(listener)

        # cache_service.stream_remove_listener(STREAM_NAME, listener)

        # print('Call cache tests:')
        # cache_service.call_create('test_call1', 'test_instance', 'test_agent', 'test_bool_stream_delegate', 'caller_agent', 'caller', True, BoolObject)
        cache_service.call_create('test_call2', 'test_instance', 'test_agent', 'test_bool_stream_delegate', 'caller_agent', 'caller', True, BoolObject)
        
        # cache_service.call_write_error('test_call1', 'Radi said it is an error!')
        # error = cache_service.call_read_error('test_call1')
        # print(error)

        cache_service.call_write_result('test_call2', 'Result here!', String)
        # error, result = cache_service.call_read_error_and_result('test_call2')
        # print(error, result)
        
        # print('Notification Service tests:')
        # k, i = await notification_service.get_call_result_notification('test_call2')
        # print(k, i)

        # notification_service.consume_notification(k)

        notification_service.start()
        time.sleep(3)
        notification_service.stop()


if __name__ == "__main__":
    asyncio.run(test())
