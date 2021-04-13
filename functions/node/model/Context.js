const uuid = require('uuid');
const IgniteClient = require('apache-ignite-client');
const ComplexObjectType = IgniteClient.ComplexObjectType;
const ObjectType = IgniteClient.ObjectType;

const Agent = require('./Agent');
const Serializer = require('../service/Serializer');

function Context (instance, fabric, state, ignite) {
  this.ignite = ignite;
  this.instance = instance;
  this.serializer = new Serializer();
  this.fabric = fabric;
  this.state = state;
  this.agent = new Agent(
    this,
    this.serializer,
    this.fabric.agentDelegate,
    this.instance.agent
  );
}

Context.prototype.generateName = function (basename = null) {
  return basename + '-' + uuid.v4();
};

Context.prototype.startAgent = async function (delegateName, parameters) {
  const agentDelegate = delegateName;
  const callDelegate = delegateName;

  const agentName = this.generateName(agentDelegate);
  const agent = new Agent(this, this.serializer, agentDelegate, agentName);

  const result = await agent.callFunction(callDelegate, parameters);
  return [agent, result];
};

Context.prototype.call = async function call (
  agentName,
  agentDelegate,
  callDelegate,
  parameters
) {
  const callsCache = await this.ignite.getOrCreateCache('calls');
  const callName = this.generateName(callDelegate);

  const compType = new ComplexObjectType(
    {
      agent: '',
      agentdelegate: '',
      delegate: '',
      calleragentdelegate: '',
      caller: '',
      finished: true,
      localtodata: true,
      error: ''
    },
    'CallData'
  );

  compType.setFieldType('agent', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('agentdelegate', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('delegate', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType(
    'calleragentdelegate',
    ObjectType.PRIMITIVE_TYPE.STRING
  );
  compType.setFieldType('caller', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('finished', ObjectType.PRIMITIVE_TYPE.BOOLEAN);
  compType.setFieldType('localtodata', ObjectType.PRIMITIVE_TYPE.BOOLEAN);
  compType.setFieldType('error', ObjectType.PRIMITIVE_TYPE.STRING);
  callsCache.setValueType(compType);

  const callData = {
    agent: agentName,
    agentdelegate: agentDelegate,
    delegate: callDelegate,
    calleragentdelegate: this.fabric.agentDelegate || '',
    caller: this.instance.instanceName,
    finished: false,
    localtodata: true,
    error: ''

    // TODO: Handle parameters
    // (parameters = (1, parameters))
  };

  await callsCache.put(callName, callData);

  console.log('Call name: ' + callName);
  const notification = await this.fabric.getCallNotification(callName);
  await this.fabric.consumeNotification(notification[0]);

  try {
    const call = await callsCache.getAndRemove(notification[1]);
    return call;
  } catch (e) {
    console.log('Exception in call to ' + callDelegate + ':');
    console.error(e);
  }
};

module.exports = Context;
