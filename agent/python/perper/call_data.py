from collections import OrderedDict
from pyignite import GenericObjectMeta
from pyignite.datatypes import String, BoolObject

def create_call_data(instance, agent, delegate, callerAgent, caller, localToData, parameters, parametersType):
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

def set_call_data_result(call_data, result, resultType):

    schema = call_data.schema
    schema['result'] = resultType

    class CallData(metaclass=GenericObjectMeta, type_name=call_data._type_name, schema=schema):
        pass

    return CallData(
        finished=True,
        result=result,
        **{key: getattr(call_data, key) for key in schema.keys() - {'finished', 'result'}}
    )