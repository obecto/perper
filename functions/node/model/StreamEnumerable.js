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

StreamEnumerable.prototype.handleNotification = async function (notification) {
  // TODO: Handle instance properly
  console.log(notification);
  if (notification[1].cache) {
    const cache = await this.stream.ignite.getOrCreateCache(notification[1].cache);
    cache.setKeyType(ObjectType.PRIMITIVE_TYPE.LONG);
    cache.setValueType(new ComplexObjectType({}));
    const value = await cache.get(notification[1].key);
    console.log(value);
    await this.stream.fabric.consumeNotification(notification[0]);
  }
};

StreamEnumerable.prototype.run = async function () {
  await this.addListener();
  // console.log("Instance name: (" + this.stream.instance.instanceName + ")");
  this.stream.fabric.getNotifications(notification =>
    this.handleNotification(notification)
  );

  // if this.stream.parameter_index < 0:
  //     this.__remove_listener()
};

StreamEnumerable.prototype.addListener = async function () {
  const streamListener = {
    agentdelegate: this.stream.fabric.agentDelegate,
    stream: this.stream.streamName,
    parameter: 0,
    filter: this.filter,
    replay: this.replay,
    localtodata: this.localToData
  };

  const streamCache = await this.stream.ignite.getOrCreateCache('streams');

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
  // TODO: Fix workaround
  Number.prototype._isSet = () => false; // eslint-disable-line no-extend-native
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
  streamCache.setValueType(compType);
  const currentValue = await streamCache.get(this.stream.streamName);
  currentValue.Listeners.push(streamListener);
  currentValue.Parameters = null;
  await streamCache.replace(this.stream.streamName, currentValue);
};

// def __modify_stream_data(self, modification):
// streams_cache = self._stream.ignite.get_cache("streams")
// current_value = streams_cache.get(self._stream.stream_name)
// new_value = modification(current_value)
// #This should be replace
// streams_cache.put(self._stream.stream_name, new_value)

// def __add_listener(self):
// stream_listener = StreamListener(
//     agentdelegate = self._stream.fabric.agent_delegate,
//     stream = self._stream.instance.instance_name,
//     parameter = self._stream.parameter_index,
//     filter = (1, self._filter),
//     replay = self.replay,
//     localtodata = self.local_to_data)

// def __add_listener_lambda_(stream_data):
//     listeners = getattr(stream_data, "listeners", getattr(stream_data, "Listeners", None))
//     listeners[1].append(stream_listener)
//     return stream_data
// return self.__modify_stream_data(__add_listener_lambda_)

module.exports = StreamEnumerable;
