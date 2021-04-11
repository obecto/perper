const StreamEnumerable = require('./StreamEnumerable');
const FilterUtils = require('./FilterUtils');
const IgniteClient = require('apache-ignite-client');
const ScanQuery = IgniteClient.ScanQuery;

// metaclass=GenericObjectMeta, type_name = "PerperStream`1[[SimpleData]]", schema=OrderedDict([
//     ("streamname", String)
// ])

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
  this.serializer = serializer;
  this.state = state;
  this.instance = instance;
  this.parameterIndex = this.instance.getStreamParameterIndex();
};

Stream.prototype.getEnumerable = function (filter, replay, localToData) {
  return new StreamEnumerable(this, filter, replay, localToData);
};

Stream.prototype.getEnumerator = function (cancellationToken) {
  return this.getEnumerable({}, false, false).getEnumerator(cancellationToken);
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

module.exports = Stream;
