const IgniteClient = require('apache-ignite-client');
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;
const ObjectType = IgniteClient.ObjectType;
const ComplexObjectType = IgniteClient.ComplexObjectType;
const FabricService = require('./FabricService');

async function test (streamNotifications) {
  const config = { fabric_host: '127.0.0.1' };
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ':10800')
  );

  const fs = new FabricService(igniteClient, config);
  const callsCache = await igniteClient.getOrCreateCache('calls');
  const callName = 'TestStream--UUID';

  const compType = new ComplexObjectType(
    {
      agent: '',
      agentdelegate: '',
      delegate: '',
      calleragentdelegate: '',
      caller: '',
      finished: true,
      localtodata: true,
      error: ''
    },
    'CallData'
  );

  compType.setFieldType('agent', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('agentdelegate', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('delegate', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType(
    'calleragentdelegate',
    ObjectType.PRIMITIVE_TYPE.STRING
  );
  compType.setFieldType('caller', ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType('finished', ObjectType.PRIMITIVE_TYPE.BOOLEAN);
  compType.setFieldType('localtodata', ObjectType.PRIMITIVE_TYPE.BOOLEAN);
  compType.setFieldType('error', ObjectType.PRIMITIVE_TYPE.STRING);
  callsCache.setValueType(compType);

  if (streamNotifications) {
    fs.getNotifications(console.log);
  } else {
    await callsCache.put(callName, {
      agent: 'Application2',
      agentdelegate: 'Application2',
      delegate: 'Application2',
      calleragentdelegate: fs.agentDelegate,
      caller: 'test_instance',
      finished: true,
      localtodata: true,
      error: ''
    });

    const notification = await fs.getCallNotification(callName);
    console.log(notification);

    await fs.consumeNotification(notification[0]);
  }
}

test(process.argv[2] !== 'call');
