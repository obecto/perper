const IgniteClient = require('apache-ignite-client');
const ComplexObjectType = IgniteClient.ComplexObjectType;

function PerperInstanceData (ignite, serializer) {
  this.ignite = ignite;
  this.serializer = serializer;

  this.nextStreamParameterIndex = 0;
  this.nextAnonymousStreamParameterIndex = 0;
  this.initialized = false;
  this.instanceName = ' ';
  this.agent = ' ';
  this.parameters = ' ';
}

PerperInstanceData.prototype.getParameters = function () {
  return this.parameters;
};

PerperInstanceData.prototype.getAgent = function () {
  return this.agent;
};

PerperInstanceData.prototype.getInstanceName = function () {
  return this.instanceName;
};

PerperInstanceData.prototype.setParameters = function (val) {
  this.parameters = val;
};

PerperInstanceData.prototype.setAgnet = function (val) {
  this.agent = val;
};

PerperInstanceData.prototype.setInstanceName = function (val) {
  this.instanceName = val;
};

PerperInstanceData.prototype.getStreamParameterIndex = function () {
  if (this.initialized) {
    this.nextStreamParameterIndex -= 1;
    return this.nextStreamParameterIndex;
  } else {
    this.nextStreamParameterIndex += 1;
    return this.nextStreamParameterIndex;
  }
};

PerperInstanceData.prototype.setTriggerValue = async function (trigger) {
  let instanceCache;
  const instanceName = trigger.InstanceName;
  this.instanceName = instanceName;

  if (trigger.IsCall) {
    instanceCache = await this.ignite.getOrCreateCache('calls');
  } else {
    instanceCache = await this.ignite.getOrCreateCache('streams');
  }

  instanceCache.setValueType(new ComplexObjectType({}, 'CallData'));
  const instanceData = await instanceCache.get(instanceName);
  this.agent = instanceData.Agent;
  this.parameters = instanceData.Parameters;
  this.initialized = true;

  console.log(instanceData);
};

module.exports = PerperInstanceData;