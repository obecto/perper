const IgniteClient = require('apache-ignite-client');
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;

const ComplexObjectType = IgniteClient.ComplexObjectType;
const ObjectArrayType = IgniteClient.ObjectArrayType;

const Serializer = require('../service/Serializer');
const PerperInstanceData = require('../cache/PerperInstanceData');
const FabricService = require('../service/FabricService');
const State = require('../model/State');
const Context = require('../model/Context');

async function perper (functions = {}) {
  const config = { fabric_host: '127.0.0.1' };
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ':10800')
  );

  /// TEST ///
  process.env.PERPER_ROOT_AGENT = 'generator';
  process.env.PERPER_AGENT_NAME = 'generator';
  /// TEST ///

  const fs = new FabricService(igniteClient, config);
  const serializer = new Serializer();
  const perperInstance = new PerperInstanceData(igniteClient, serializer);
  const state = new State(perperInstance, igniteClient, serializer);
  const context = new Context(perperInstance, fs, state, igniteClient, serializer);

  fs.getNotifications(async notification => {
    if (notification[1] && notification[1].delegate && notification[1].call) {
      const cache = await igniteClient.getOrCreateCache('calls');
      const callDataType = FabricService.generateCallDataType();
      callDataType.setFieldType('Parameters', new ObjectArrayType());

      cache.setValueType(callDataType);

      const callData = await cache.get(notification[1].call);
      if (functions[callData.Delegate]) {
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
          callDataType.setFieldType('Parameters', new ComplexObjectType({}));
          await cache.replace(notification[1].call, callData);
        } catch {
          callDataType.setFieldType('Parameters', new ObjectArrayType());
          await cache.replace(notification[1].call, callData);
        }
      }

      fs.consumeNotification(notification, false /* log */);
    }
  });

  return context;
}

module.exports = perper;
