const StreamEnumerable = require('./StreamEnumerable');
const FilterUtils = require('./FilterUtils');
const IgniteClient = require('apache-ignite-client');
const ScanQuery = IgniteClient.ScanQuery;

function Stream () {}

Stream.prototype.setParameters = function (streamName, ignite, fabric) {
  this.fabric = fabric;
  this.ignite = ignite;
  this.streamName = streamName;

  if (arguments.length === 6) {
    this.setAdditionalParameters(
      arguments[3], // serializer
      arguments[4], // state
      arguments[5] // instance
    );
  }

  this.functionName = null;
};

Stream.prototype.setAdditionalParameters = function (
  serializer,
  state,
  instance
) {
  if (serializer) this.serializer = serializer;
  if (state) this.state = state;
  if (instance) {
    this.instance = instance;
    this.parameterIndex = this.instance.getStreamParameterIndex();
  }
};

Stream.prototype.dataLocal = function () {
  return this.getEnumerable({}, false, true);
};

Stream.prototype.filter = function (filter, dataLocal = false) {
  return this.getEnumerable(
    FilterUtils.convertFilter(filter),
    false,
    dataLocal
  );
};

Stream.prototype.getEnumerable = function (filter, replay, localToData) {
  return new StreamEnumerable(this, filter, replay, localToData);
};

Stream.prototype.replay = function (data_local = false) {
  // TODO: Implement kwargs
  // if len(kwargs > 0): return this.getEnumerable(FilterUtils.convertFilter(kwargs['filter']), true, data_local)
  return this.getEnumerable({}, true, data_local);
};

Stream.prototype.query = async function (query, callback = null) {
  const cache = this.ignite.getOrCreateCache(this.streamName);
  const cursor = await cache.query(new ScanQuery());
  const cacheItems = await cursor.getAll();
  // const queryable = query(cacheItems);

  for (const cacheEntry of cacheItems) {
    if (callback) {
      callback(cacheEntry.getValue());
    } else {
      console.log(cacheEntry.getValue());
    }
  }
};

Stream.getStreamListenerConfig = function () {
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

module.exports = Stream;
