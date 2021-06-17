from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.utils import entity_id
from pyignite.datatypes import String, IntObject, BoolObject, MapObject, EnumObject, CollectionObject

StreamDelegateTypeId = entity_id("StreamDelegateType")

def create_stream_data(instance, agent, delegate, delegateType, ephemeral, parameters, parametersType, indexType=None, indexFields=None):
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

def create_stream_listener(callerAgent, caller, parameter, replay, localToData, filter={}):
    return StreamListener(
        callerAgent=callerAgent,
        caller=caller,
        parameter=parameter,
        replay=replay,
        localToData=localToData,
        filter=(MapObject.HASH_MAP, filter)
    )

def stream_data_add_listener(streamData, streamListener):
    StreamData = type(streamData)

    new_listeners = streamData.listeners[1][:]
    new_listeners.append(streamListener)

    return StreamData(
        listeners=(CollectionObject.ARR_LIST, new_listeners),
        **{key: getattr(streamData, key) for key in streamData._schema.keys() - {'listeners'}}
    )

def stream_data_remove_listener(streamData, streamListener):
    StreamData = type(streamData)

    new_listeners = streamData.listeners[1][:]
    new_listeners.remove(streamListener)

    return StreamData(
        listeners=(CollectionObject.ARR_LIST, new_listeners),
        **{key: getattr(streamData, key) for key in streamData._schema.keys() - {'listeners'}}
    )
