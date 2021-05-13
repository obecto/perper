const IgniteClient = require("apache-ignite-client");
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;

const ComplexObjectType = IgniteClient.ComplexObjectType;
const ObjectArrayType = IgniteClient.ObjectArrayType;
const ObjectType = IgniteClient.ObjectType;

const Serializer = require("../service/Serializer");
const PerperInstanceData = require("../cache/PerperInstanceData");
const FabricService = require("../service/FabricService");
const State = require("../model/State");
const Context = require("../model/Context");
const Stream = require("../model/Stream");

async function listenNotifications (fs, igniteClient, perperInstance, functions) {
  for await (let notification of fs.getNotifications(console.log)) {
    if (notification[1] && notification[1].delegate) {
      let notValue, cache, dataType;

      if (notification[1].stream) {
        notValue = notification[1].stream;
        cache = await igniteClient.getOrCreateCache("streams");
        dataType = Stream.generateStreamDataType();
      } else {
        notValue = notification[1].call;
        cache = await igniteClient.getOrCreateCache("calls");
        dataType = FabricService.generateCallDataType();
      }

      cache.setKeyType(ObjectType.PRIMITIVE_TYPE.STRING);
      let data = {};

      try {
        dataType.setFieldType("Parameters", new ObjectArrayType());
        cache.setValueType(dataType);
        data = await cache.get(notValue);
        if (data.Parameters.some(x => x instanceof IgniteClient.BinaryObject)) throw new Error('Got non-serialized buffer.');
      } catch {
        dataType.setFieldType("Parameters", new ObjectArrayType(new ComplexObjectType({})));
        cache.setValueType(dataType);
        data = await cache.get(notValue);
      }

      if (functions[data.Delegate]) {
        const parameters = await perperInstance.setTriggerValue(
          data,
          functions[data.Delegate].parameters,
          false /* log */
        );

        if (
          parameters instanceof Array &&
          functions[data.Delegate].mapArrayToParams !== false
        ) {
          functions[data.Delegate].action.apply(this, parameters);
        } else {
          functions[data.Delegate].action.call(this, parameters);
        }

        data.Finished = true;
      }

      fs.consumeNotification(notification, false /* log */);
    }
  }
}

async function perper(functions = {}) {
  const config = { fabric_host: "127.0.0.1" };
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ":10800")
  );

  const fs = new FabricService(igniteClient, config);
  const serializer = new Serializer();
  const perperInstance = new PerperInstanceData(igniteClient, serializer);
  const state = new State(perperInstance, igniteClient, serializer);
  const context = new Context(
    perperInstance,
    fs,
    state,
    igniteClient,
    serializer
  );

  listenNotifications(fs, igniteClient, perperInstance, functions);
  return context;
}

module.exports = perper;
