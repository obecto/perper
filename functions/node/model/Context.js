const uuid = require('uuid');
const Agent = require('./Agent');
const FabricService = require('../service/FabricService');
const Stream = require('../model/Stream');
const createEnumItem = require('../utils/createEnumItem');

// TODO: Fill up some missing Context methods.
function Context (instance, fabric, state, ignite, serializer) {
  this.ignite = ignite;
  this.instance = instance;
  this.serializer = serializer;
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

StreamDelegateType = {
  function: createEnumItem('streamdelegatetype', 0),
  action: createEnumItem('streamdelegatetype', 1),
  external: createEnumItem('streamdelegatetype', 2)
}

Context.prototype.streamFunction = function (
  functionName,
  parameters,
  flags
) {
  const streamName = this.generateName(functionName);
  this.createStream(streamName, StreamDelegateType.function, functionName, parameters, null, flags);

  return new Stream(); // streamname=stream_name
}

Context.prototype.createStream = async function call (
  streamName,
  delegateType,
  delegateName,
  parameters,
  type,
  flags
) {
  const streamsCache = await this.ignite.getOrCreateCache('streams');
  const compType = Stream.generateStreamDataType();
  streamsCache.setValueType(compType);

  // TODO: Implemnt flags.
  const streamData = {
    // Agent: this.fabric.agentDelegate,
    AgentDelegate: this.fabric.agentDelegate,
    Delegate: delegateName,
    DelegateType: delegateType, // delegatetype=(entity_id("StreamDelegateType"), delegate_type.value)
    Parameters: parameters, // parameters=ParameterData(parameters=(1, parameters)),
    Listeners: [],
    IndexType: null, // (PerperTypeUtils.get_java_type_name(type_) or type_.name),
    IndexFields: null, // (PerperTypeUtils.get_java_type_name(type_) or type_.name) if ((flags and StreamFlags.query) != 0 and type_ != None) else None
    Ephemeral: false // (flags and StreamFlags.ephemeral) != 0
  };

  await streamsCache.put(streamName, streamData);
  console.log('Stream name: ' + streamName);
};

Context.prototype.call = async function call (
  agentName,
  agentDelegate,
  callDelegate,
  parameters
) {
  const callsCache = await this.ignite.getOrCreateCache('calls');
  const callName = this.generateName(callDelegate);

  const compType = FabricService.generateCallDataType();
  callsCache.setValueType(compType);

  const callData = {
    Agent: agentName,
    AgentDelegate: agentDelegate,
    Delegate: callDelegate,
    CallerAgentDelegate: this.fabric.agentDelegate || '',
    Caller: this.instance.instanceName,
    Finished: false,
    LocalToData: true,
    Error: '',
    Parameters: parameters // Be careful of what type the Parameters are.
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
