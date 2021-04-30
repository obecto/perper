const IgniteClient = require('apache-ignite-client');
const Stream = require('./Stream');
const ComplexObjectType = IgniteClient.ComplexObjectType;
const ObjectType = IgniteClient.ObjectType;

function StreamEnumerable (stream, filter, localToData, replay) {
  this.stream = stream;
  this.localToData = localToData;
  this.filter = filter;
  this.replay = replay;
}

StreamEnumerable.prototype.handleNotification = async function (notification, callback) {
  if (notification[1].cache) {
    const cache = await this.stream.ignite.getOrCreateCache(notification[1].cache);
    cache.setKeyType(ObjectType.PRIMITIVE_TYPE.LONG);
    cache.setValueType(new ComplexObjectType({}));
    const value = await cache.get(notification[1].key);
    const serialized = this.stream.serializer.serialize(value);
    callback.call(this, serialized);
    await this.stream.fabric.consumeNotification(notification[0]);
  }
};

StreamEnumerable.prototype.run = async function (callback) {
  await this.addListener();
  this.stream.fabric.getNotifications(notification =>
    this.handleNotification(notification, callback)
  );

  if (this.stream.parameterIndex < 0) await this.removeListener();
};

StreamEnumerable.prototype.addListener = async function () {
  const streamCache = await this.stream.ignite.getOrCreateCache('streams');
  const streamListener = {
    agentdelegate: this.stream.fabric.agentDelegate,
    stream: this.stream.streamName,
    parameter: 0,
    filter: this.filter,
    replay: this.replay,
    localtodata: this.localToData
  };

  streamCache.setValueType(Stream.generateStreamDataType());
  const currentValue = await streamCache.get(this.stream.streamName);
  currentValue.Listeners.push(streamListener);
  currentValue.Parameters = null;
  await streamCache.replace(this.stream.streamName, currentValue);
};

StreamEnumerable.prototype.removeListener = async function () {
  const streamCache = await this.stream.ignite.getOrCreateCache('streams');
  streamCache.setValueType(Stream.generateStreamDataType());
  const currentValue = await streamCache.get(this.stream.streamName);

  if (currentValue.Listeners) {
    currentValue.Listeners.forEach((el, i) => {
      if (el.stream === this.stream.streamName) currentValue.Listeners.splice(i, 1);
    });
  }

  currentValue.Parameters = null;
  await streamCache.replace(this.stream.streamName, currentValue);
};

module.exports = StreamEnumerable;
