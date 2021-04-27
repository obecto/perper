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

PerperInstanceData.prototype.setTriggerValue = async function (instanceData, type, log = true) {
  const deserialized = this.serializer.deserialize(instanceData.Parameters, type, log);
  this.initialized = true;
  this.agent = instanceData.Agent;
  this.parameters = deserialized;
  return deserialized;
};

module.exports = PerperInstanceData;
