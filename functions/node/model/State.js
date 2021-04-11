const IgniteClient = require('apache-ignite-client');
const ComplexObjectType = IgniteClient.ComplexObjectType;

const State = function (instance, ignite, serializer) {
  this.agent = instance.agent;
  this.ignite = ignite;
  this.serializer = serializer;
};

State.prototype.getValue = async function (key, defaultValueFactory) {
  const cache = await this.ignite.getOrCreateCache(this.agent);
  cache.setValueType(new ComplexObjectType({}));

  const result = await cache.get(key);
  if (!result) {
    const defaultValue = defaultValueFactory();
    await cache.put(key, this.serializer.serialize(defaultValue));
    return defaultValue;
  }

  return this.serializer.deserialize(result);
};

State.prototype.setValue = async function (key, value) {
  const cache = await this.ignite.getOrCreateCache(this.agent);
  return cache.put(key, this.serializer.serialize(value));
};

module.exports = State;
