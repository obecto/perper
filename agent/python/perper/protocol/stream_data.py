from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes.complex import ObjectArrayObject
from pyignite.utils import entity_id
from pyignite.datatypes import String, IntObject, BoolObject, MapObject, EnumObject, CollectionObject

StreamDelegateTypeId = entity_id("StreamDelegateType")
def create_stream_data(instance, agent, delegate, delegate_type, ephemeral, parameters, index_type=None, index_fields=None):
    class StreamData(metaclass=GenericObjectMeta, type_name=f"StreamData_{agent}_{delegate}", schema=OrderedDict([
        ('instance', String),
        ('agent', String),
        ('delegate', String),
        ('delegateType', EnumObject),
        ('indexType', String),
        ('indexFields', MapObject),
        ('ephemeral', BoolObject),
        ('listeners', CollectionObject),
        ('parameters', ObjectArrayObject),
    ])):
        pass

    return StreamData(
        instance=instance,
        agent=agent,
        delegate=delegate,
        delegateType=(StreamDelegateTypeId, delegate_type),
        indexType=index_type,
        indexFields=None if index_fields is None else (MapObject.HASH_MAP, index_fields),
        ephemeral=ephemeral,
        listeners=(CollectionObject.ARR_LIST, []),
        parameters=(ObjectArrayObject.OBJECT,parameters)
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

def create_stream_listener(caller_agent, caller, parameter, replay, local_to_data, filter={}):
    return StreamListener(
        callerAgent=caller_agent,
        caller=caller,
        parameter=parameter,
        replay=replay,
        localToData=local_to_data,
        filter=(MapObject.HASH_MAP, filter)
    )

def stream_data_add_listener(stream_data, stream_listener):
    StreamData = type(stream_data)

    new_listeners = stream_data.listeners[1][:]
    new_listeners.append(stream_listener)

    return StreamData(
        listeners=(CollectionObject.ARR_LIST, new_listeners),
        **{key: getattr(stream_data, key) for key in stream_data._schema.keys() - {'listeners'}}
    )

def stream_data_remove_listener(stream_data, stream_listener):
    StreamData = type(stream_data)

    new_listeners = stream_data.listeners[1][:]
    new_listeners.remove(stream_listener)

    return StreamData(
        listeners=(CollectionObject.ARR_LIST, new_listeners),
        **{key: getattr(stream_data, key) for key in stream_data._schema.keys() - {'listeners'}}
    )

def stream_data_remove_listener_caller(stream_data, caller, parameter):
    StreamData = type(stream_data)
    listeners = stream_data.listeners[1][:]

    for listener in listeners:
        if listener.caller == caller and listener.parameter == parameter:
            listeners.remove(listener)
            break

    return StreamData(
        listeners=(CollectionObject.ARR_LIST, listeners),
        **{key: getattr(stream_data, key) for key in stream_data._schema.keys() - {'listeners'}}
    )
