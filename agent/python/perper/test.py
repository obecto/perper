from pyignite import Client, GenericObjectMeta
from collections import OrderedDict
from pyignite.datatypes import String, BoolObject

from call import createCallData, setCallDataResult
from stream import createStreamData, createStreamListener, streamDataAddListener, streamDataRemoveListener

ignite = Client()
with ignite.connect('127.0.0.1', 10800):
    numbers = ignite.get_or_create_cache('numbers')
    class PerperStream(metaclass=GenericObjectMeta, schema=OrderedDict([
        ('stream', String)
    ])):
        pass

    numbers.put('abc', PerperStream(
        stream='hah'
    ))
    result = numbers.get('xyz')
    print(result)
    result = numbers.get('abc')
    print(result)
    calls = ignite.get_cache('calls')

    result = calls.get('testCall1')
    print(result)

    callData = calls.get('testCall2')
    print(callData)
    if callData is not None:
        callData = setCallDataResult(callData, (MapObject.HASH_MAP, {(1, LongObject): 2, (1, IntObject): 3}), MapObject)
        calls.put('testCall2', callData)

    calls.put_if_absent('testCall3', createCallData(
        instance="testInstance",
        agent="testAgent",
        delegate="testPyBoolFunctionDelegate",
        callerAgent="testAgent",
        caller="testCaller",
        localToData=False,
        parameters=False,
        parametersType=BoolObject
    ))

    streams = ignite.get_or_create_cache('streams')

    streamData = streams.get('testStream1')
    if streamData is not None:
        streamData = streamDataAddListener(streamData, createStreamListener(
            callerAgent="testAgent",
            caller="testStream3",
            parameter=4,
            replay=False,
            localToData=False,
            filter={}
        ))
        streams.put('testStream1', streamData)

    streams.put_if_absent('testStream3', createStreamData(
        instance="testInstance",
        agent="testAgent",
        delegate="testPyBoolStreamDelegate",
        delegateType=1,
        ephemeral=False,
        parameters=True,
        parametersType=BoolObject
    ))

    print(streams.get('testStream1'))
    print(streams.get('testStream3'))
