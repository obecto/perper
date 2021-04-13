// const Context = require('./Context');
// const Serializer = require('../service/Serializer');

/**
 * @class {Agent} Agent
 * @param {Context} context Context
 * @param {Serializer} serializer Serializer
 * @param {String} agentName The agent name
 * @param {String} agentDelegate The agent delegate
 */
function Agent (context, serializer, agentDelegate, agentName) {
  if (arguments.length === 2) {
    this.context = context;
    this.serializer = serializer;
  } else if (arguments.length === 4) {
    this.context = context;
    this.serializer = serializer;
    this.agentName = agentName;
    this.agentDelegate = agentDelegate;
  } else {
    throw new Error('Arguments should be either 2 or 4');
  }
}

Agent.prototype.callFunction = async function (functionName, parameters) {
  const callData = await this.context.call(
    this.agentName,
    this.agentDelegate,
    functionName,
    parameters
  );

  console.log(callData);
  if (!callData.result) return null;
  return this.serializer.deserialize(callData.result);
};

Agent.prototype.callAction = async function (actionName, parameters) {
  const result = await this.context.call(
    this.agentName,
    this.agentDelegate,
    actionName,
    parameters
  );

  return result;
};

module.exports = Agent;
