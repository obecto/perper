const IgniteClient = require('apache-ignite-client');
const EnumItem = IgniteClient.EnumItem;
const MapObjectType = IgniteClient.MapObjectType;
const CollectionObjectType = IgniteClient.CollectionObjectType;
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

StreamEnumerable.prototype.getStreamListenerConfig = function () {
  const listenerTypes = new ComplexObjectType(
    {
      agentdelegate: this.stream.fabric.agentDelegate,
      stream: this.stream.streamName,
      parameter: 0,
      filter: this.filter,
      replay: false,
      localtodata: false
    },
    'StreamListener'
  );

  listenerTypes.setFieldType('agentdelegate', ObjectType.PRIMITIVE_TYPE.STRING);
  listenerTypes.setFieldType('stream', ObjectType.PRIMITIVE_TYPE.STRING);
  listenerTypes.setFieldType('parameter', ObjectType.PRIMITIVE_TYPE.INTEGER);
  listenerTypes.setFieldType('filter', new MapObjectType());
  listenerTypes.setFieldType('replay', ObjectType.PRIMITIVE_TYPE.BOOLEAN);
  listenerTypes.setFieldType('localtodata', ObjectType.PRIMITIVE_TYPE.BOOLEAN);

  const compType = new ComplexObjectType(
    {
      Agent: '',
      AgentDelegate: '',
      Delegate: '',
      DelegateType: new EnumItem(-738053697),
      Parameters: null,
      Listeners: [],
      IndexType: null,
      IndexFields: null,
      Ephemeral: true
    },
    'StreamData'
  );

  compType.setFieldType('Agent', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('AgentDelegate', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('Delegate', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('DelegateType', ObjectType.PRIMITIVE_TYPE.ENUM);
  compType.setFieldType('Parameters', new ComplexObjectType({}));
  compType.setFieldType(
    'Listeners',
    new CollectionObjectType(
      CollectionObjectType.COLLECTION_SUBTYPE.ARRAY_LIST,
      listenerTypes
    )
  );
  compType.setFieldType('IndexType', new ComplexObjectType({})); // FIXME: is actually nullable string
  compType.setFieldType('IndexFields', new ComplexObjectType({})); // FIXME: is actually nullable dict
  compType.setFieldType('Ephemeral', ObjectType.PRIMITIVE_TYPE.BOOLEAN);

  return compType;
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

  streamCache.setValueType(this.getStreamListenerConfig());
  const currentValue = await streamCache.get(this.stream.streamName);
  currentValue.Listeners.push(streamListener);
  currentValue.Parameters = null;
  await streamCache.replace(this.stream.streamName, currentValue);
};

StreamEnumerable.prototype.removeListener = async function () {
  const streamCache = await this.stream.ignite.getOrCreateCache('streams');
  streamCache.setValueType(this.getStreamListenerConfig());
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
