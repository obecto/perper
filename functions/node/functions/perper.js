const IgniteClient = require("apache-ignite-client");
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;

const ComplexObjectType = IgniteClient.ComplexObjectType;
const ObjectArrayType = IgniteClient.ObjectArrayType;

const Serializer = require("../service/Serializer");
const PerperInstanceData = require("../cache/PerperInstanceData");
const FabricService = require("../service/FabricService");

async function perper(functions = {}) {
  const config = { fabric_host: "127.0.0.1" };
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ":10800")
  );

  const fs = new FabricService(igniteClient, config, Object.keys(functions)[0]);
  fs.getNotifications(async notification => {
    if (notification[1] && notification[1].delegate && notification[1].call) {
      const cache = await igniteClient.getOrCreateCache("calls");
      const callDataType = fs.generateCallDataType();

      cache.setValueType(callDataType);

      const callData = await cache.get(notification[1].call);
      if (functions[callData.Delegate]) {
        const perperInstance = new PerperInstanceData(
          igniteClient,
          new Serializer()
        );

        const parameters = await perperInstance.setTriggerValue(
          callData,
          functions[callData.Delegate].parameters,
          false /* log */
        );

        if (
          parameters instanceof Array &&
          functions[callData.Delegate].mapArrayToParams !== false
        ) {
          functions[callData.Delegate].action.apply(this, parameters);
        } else {
          functions[callData.Delegate].action.call(this, parameters);
        }

        callData.Finished = true;

        try {
          callDataType.setFieldType("Parameters", new ComplexObjectType({}));
          await cache.replace(notification[1].call, callData);
        } catch {
          callDataType.setFieldType("Parameters", new ObjectArrayType());
          await cache.replace(notification[1].call, callData);
        }
      }

      fs.consumeNotification(notification, false /* log */);
    }
  });
}

module.exports = perper;
