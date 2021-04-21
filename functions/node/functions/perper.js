const IgniteClient = require('apache-ignite-client');
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;
const ObjectType = IgniteClient.ObjectType;
const ComplexObjectType = IgniteClient.ComplexObjectType;

const Serializer = require("../service/Serializer");
const PerperInstanceData = require("../cache/PerperInstanceData");
const FabricService = require('../service/FabricService');

async function perper(input, type, callback, mapArrayToParams = true) {
  const config = { fabric_host: '127.0.0.1' };
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ':10800')
  );

  const fs = new FabricService(igniteClient, config);
  fs.getNotifications(async notification => {
    console.log(notification);
    if (notification[1] && notification[1].delegate && notification[1].call) {
      const cache = await igniteClient.getOrCreateCache('calls');
      cache.setValueType(new ComplexObjectType({}));
      const callData = await cache.get(notification[1].call);
      console.log(callData);
      fs.consumeNotification(notification);
    }
  });

  const perperInstance = new PerperInstanceData(igniteClient, new Serializer());
  // const parameters = await perperInstance.setTriggerValue(input, type);

  // if (parameters instanceof Array && mapArrayToParams) {
  //   callback.apply(this, parameters);
  // } else {
  //   callback.call(this, parameters);
  // }
};

module.exports = perper;
