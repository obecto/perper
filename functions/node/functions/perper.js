const IgniteClient = require("apache-ignite-client");
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;
const ObjectType = IgniteClient.ObjectType;
const ComplexObjectType = IgniteClient.ComplexObjectType;
const ObjectArrayType = IgniteClient.ObjectArrayType;

const Serializer = require("../service/Serializer");
const PerperInstanceData = require("../cache/PerperInstanceData");
const FabricService = require("../service/FabricService");

async function perper(functions = {}, mapArrayToParams = true) {
  const config = { fabric_host: "127.0.0.1" };
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ":10800")
  );

  const fs = new FabricService(igniteClient, config, Object.keys(functions)[0]);
  fs.getNotifications(async notification => {
    console.log(notification);
    if (notification[1] && notification[1].delegate && notification[1].call) {
      const cache = await igniteClient.getOrCreateCache("calls");

      const compType = new ComplexObjectType(
        {
          Agent: "",
          AgentDelegate: "",
          Delegate: "",
          CallerAgentDelegate: "",
          Caller: "",
          Finished: true,
          LocalToData: true,
          Error: "",
          Parameters: new ComplexObjectType({})
        },
        "CallData"
      );

      compType.setFieldType("Agent", ObjectType.PRIMITIVE_TYPE.STRING);
      compType.setFieldType("AgentDelegate", ObjectType.PRIMITIVE_TYPE.STRING);
      compType.setFieldType("Delegate", ObjectType.PRIMITIVE_TYPE.STRING);
      compType.setFieldType(
        "CallerAgentDelegate",
        ObjectType.PRIMITIVE_TYPE.STRING
      );
      compType.setFieldType("Caller", ObjectType.PRIMITIVE_TYPE.STRING);
      compType.setFieldType("Finished", ObjectType.PRIMITIVE_TYPE.BOOLEAN);
      compType.setFieldType("LocalToData", ObjectType.PRIMITIVE_TYPE.BOOLEAN);
      compType.setFieldType("Error", new ComplexObjectType({})); // FIXME: is actually nullable string
      compType.setFieldType("Parameters", new ObjectArrayType());
      cache.setValueType(compType);

      const callData = await cache.get(notification[1].call);
      if (functions[callData.Delegate]) {
        const perperInstance = new PerperInstanceData(
          igniteClient,
          new Serializer()
        );
        const parameters = await perperInstance.setTriggerValue(
          callData,
          functions[callData.Delegate].parameters
        );

        if (parameters instanceof Array && mapArrayToParams) {
          functions[callData.Delegate].action.apply(this, parameters);
        } else {
          functions[callData.Delegate].action.call(this, parameters);
        }

        callData.Finished = true;
        cache.replace(notification[1].call, callData);
      }

      fs.consumeNotification(notification);
    }
  });
}

module.exports = perper;
