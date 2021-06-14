from collections import OrderedDict
from pyignite import Client, GenericObjectMeta
from pyignite.utils import entity_id
from pyignite.datatypes import String, IntObject, LongObject, BoolObject, MapObject, EnumObject, CollectionObject

StreamDelegateTypeId = entity_id("StreamDelegateType")

def createStreamData(instance, agent, delegate, delegateType, ephemeral, parameters, parametersType, indexType=None, indexFields=None):
    class StreamData(metaclass=GenericObjectMeta, type_name=f"StreamData_{agent}_{delegate}", schema=OrderedDict([
        ('instance', String),
        ('agent', String),
        ('delegate', String),
        ('delegateType', EnumObject),
        ('indexType', String),
        ('indexFields', MapObject),
        ('ephemeral', BoolObject),
        ('listeners', CollectionObject),
        ('parameters', parametersType),
    ])):
        pass

    return StreamData(
        instance=instance,
        agent=agent,
        delegate=delegate,
        delegateType=(StreamDelegateTypeId, delegateType),
        indexType=indexType,
        indexFields=None if indexFields is None else (MapObject.HASH_MAP, indexFields),
        ephemeral=ephemeral,
        listeners=(CollectionObject.ARR_LIST, []),
        parameters=parameters
    )

class StreamListener(metaclass=GenericObjectMeta, schema=OrderedDict([
    ('callerAgent', String),
    ('caller', String),
    ('parameter', IntObject),
    ('filter', MapObject),
    ('replay', BoolObject),
    ('localToData', BoolObject),
])):
    pass

def createStreamListener(callerAgent, caller, parameter, replay, localToData, filter={}):
    return StreamListener(
        callerAgent=callerAgent,
        caller=caller,
        parameter=parameter,
        replay=replay,
        localToData=localToData,
        filter=(MapObject.HASH_MAP, filter)
    )

def streamDataAddListener(streamData, streamListener):
    StreamData = type(streamData)

    new_listeners = streamData.listeners[1][:]
    new_listeners.append(streamListener)

    return StreamData(
        listeners=(CollectionObject.ARR_LIST, new_listeners),
        **{key: getattr(streamData, key) for key in streamData._schema.keys() - {'listeners'}}
    )

def streamDataRemoveListener(streamData, streamListener):
    StreamData = type(streamData)

    new_listeners = streamData.listeners[1][:]
    new_listeners.remove(streamListener)

    return StreamData(
        listeners=(CollectionObject.ARR_LIST, new_listeners),
        **{key: getattr(streamData, key) for key in streamData._schema.keys() - {'listeners'}}
    )

def createCallData(instance, agent, delegate, callerAgent, caller, localToData, parameters, parametersType):
    class CallData(metaclass=GenericObjectMeta, type_name=f"CallData_{agent}_{delegate}", schema=OrderedDict([
        ('instance', String),
        ('agent', String),
        ('delegate', String),
        ('callerAgent', String),
        ('caller', String),
        ('finished', BoolObject),
        ('localToData', BoolObject),
        ('parameters', parametersType),
        #('result', resultType),
    ])):
        pass

    return CallData(
        instance=instance,
        agent=agent,
        delegate=delegate,
        callerAgent=callerAgent,
        caller=caller,
        finished=False,
        localToData=localToData,
        parameters=parameters
    )

def setCallDataResult(call_data, result, resultType):

    schema = call_data.schema
    schema['result'] = resultType

    class CallData(metaclass=GenericObjectMeta, type_name=call_data._type_name, schema=schema):
        pass

    return CallData(
        finished=True,
        result=result,
        **{key: getattr(call_data, key) for key in schema.keys() - {'finished', 'result'}}
    )

ignite = Client()
with ignite.connect('127.0.0.1', 10800):
    numbers = ignite.get_or_create_cache('numbers')
    class RawStream(metaclass=GenericObjectMeta, schema=OrderedDict([
        ('streamName', String)
    ])):
        pass

    numbers.put('abc', RawStream(
        streamName='hah'
    ))
    result = numbers.get('xyz')
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
