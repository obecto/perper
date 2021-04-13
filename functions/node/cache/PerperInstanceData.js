function PerperInstanceData (ignite, serializer) {
  this.signite = ignite;
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

PerperInstanceData.prototype.setTriggerValue = function (trigger) {
  // TODO: Implement caches
  if (trigger.includes('Call')) {
    this.instanceName = trigger.Call;
    // callsCache = this.ignite.getOrCreateCache('calls');
  } else {
    this.instanceName = trigger.Stream;
    // streamsCache = this.ignite.getOrCreateCache('streams');
  }

  // const instanceDataBinary = instanceCache.get(instanceName);
  // this.agent = instanceDataBinary.agent;
  // this.parameters = instanceDataBinary.parameters;

  this.initialized = true;
};
