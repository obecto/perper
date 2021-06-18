from pyignite import Client, GenericObjectMeta
from collections import OrderedDict
from pyignite.datatypes import String, BoolObject

from thin_client import PerperIgniteClient
from call_data import create_call_data, set_call_data_result
from stream_data import create_stream_data, create_stream_listener, stream_data_add_listener, stream_data_remove_listener
from cache_service import CacheService

ignite = PerperIgniteClient()
with ignite.connect('127.0.0.1', 10800):
    # numbers = ignite.get_or_create_cache('numbers')
    # class PerperStream(metaclass=GenericObjectMeta, schema=OrderedDict([
    #     ('stream', String)
    # ])):
    #     pass

    # numbers.put('abc', PerperStream(
    #     stream='hah'
    # ))
    # result = numbers.get('xyz')
    # print(result)
    # result = numbers.get('abc')
    # print(result)
    # calls = ignite.get_cache('calls')

    # result = calls.get('testCall1')
    # print(result)

    # callData = calls.get('testCall2')
    # print(callData)
    # if callData is not None:
    #     callData = set_call_data_result(callData, (MapObject.HASH_MAP, {(1, LongObject): 2, (1, IntObject): 3}), MapObject)
    #     calls.put('testCall2', callData)

    # calls.put_if_absent('testCall3', create_call_data(
    #     instance="testInstance",
    #     agent="testAgent",
    #     delegate="testPyBoolFunctionDelegate",
    #     callerAgent="testAgent",
    #     caller="testCaller",
    #     localToData=False,
    #     parameters=False,
    #     parametersType=BoolObject
    # ))

    # streams = ignite.get_or_create_cache('streams')

    # streamData = streams.get('testStream1')
    # if streamData is not None:
    #     streamData = stream_data_add_listener(streamData, create_stream_listener(
    #         callerAgent="testAgent",
    #         caller="testStream3",
    #         parameter=4,
    #         replay=False,
    #         localToData=False,
    #         filter={}
    #     ))
    #     streams.put('testStream1', streamData)

    # streams.put_if_absent('testStream3', create_stream_data(
    #     instance="testInstance",
    #     agent="testAgent",
    #     delegate="testPyBoolStreamDelegate",
    #     delegateType=1,
    #     ephemeral=False,
    #     parameters=True,
    #     parametersType=BoolObject
    # ))

    # print(streams.get('testStream1'))
    # print(streams.get('testStream3'))

    cache_service = CacheService(ignite)
    stream = cache_service.stream_create('test', 'test_instance', 'test_agent', 'test_bool_stream_delegate', 1, BoolObject, True, ephemeral = False)

    key = cache_service.stream_write_item('test', 'Hello world')
    item = cache_service.stream_read_item('test', key)
    print(key, item)
