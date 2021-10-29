from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, BoolObject
from pyignite.datatypes.complex import ObjectArrayObject

def create_call_data(instance, agent, delegate, caller_agent, caller, local_to_data, parameters):
    class CallData(metaclass=GenericObjectMeta, type_name=f"CallData_{agent}_{delegate}", schema=OrderedDict([
        ('instance', String),
        ('agent', String),
        ('delegate', String),
        ('callerAgent', String),
        ('caller', String),
        ('finished', BoolObject),
        ('localToData', BoolObject),
        ('parameters', ObjectArrayObject),
        #('result', result_type),
    ])):
        pass

    return CallData(
        instance=instance,
        agent=agent,
        delegate=delegate,
        callerAgent=caller_agent,
        caller=caller,
        finished=False,
        localToData=local_to_data,
        parameters=(ObjectArrayObject.OBJECT, parameters)
    )

def set_call_data_result(call_data, result, result_type):
    class CallData(metaclass=GenericObjectMeta, type_name=call_data._type_name, schema=OrderedDict([
        *call_data.schema.items(),
        ('result', result_type)
    ])):
        pass

    return CallData(
        finished=True,
        result=result,
        **{key: getattr(call_data, key) for key in call_data.schema.keys() - {'finished', 'result'}}
    )

def set_call_data_error(call_data, error):
    class CallData(metaclass=GenericObjectMeta, type_name=call_data._type_name, schema=OrderedDict([
        *call_data.schema.items(),
        ('error', String)
    ])):
        pass

    return CallData(
        finished=True,
        error=error,
        **{key: getattr(call_data, key) for key in call_data.schema.keys() - {'finished', 'error'}}
    )

def set_call_data_finished(call_data):
    class CallData(metaclass=GenericObjectMeta, type_name=call_data._type_name, schema=call_data.schema):
        pass

    return CallData(finished=True, **{key: getattr(call_data, key) for key in call_data.schema.keys() - {'finished', 'error'}})
