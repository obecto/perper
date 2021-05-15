const IgniteClient = require('apache-ignite-client');
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;
const FabricService = require('./FabricService');

async function listenNotifications (asyncGenerator) {
  for await (const notification of asyncGenerator) {
    console.log(notification);
  }
}

async function test (streamNotifications) {
  const config = { fabric_host: '127.0.0.1' };
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ':10800')
  );

  process.env.PERPER_ROOT_AGENT = 'Application2';
  process.env.PERPER_AGENT_NAME = 'Application2';
  const fs = new FabricService(igniteClient, config);
  const callsCache = await igniteClient.getOrCreateCache('calls');
  const callName = 'TestStream--UUID';

  const compType = FabricService.generateCallDataType();
  callsCache.setValueType(compType);

  if (streamNotifications) {
    const asyncGenerator = fs.getNotifications(console.log);
    listenNotifications(asyncGenerator);
  } else {
    await callsCache.put(callName, {
      Agent: 'Application2',
      AgentDelegate: 'Application2',
      Delegate: 'Application2',
      CallerAgentDelegate: fs.agentDelegate,
      Caller: 'test_instance',
      Finished: true,
      Parameters: null,
      LocalToData: true,
      Error: ''
    });

    const notification = await fs.getCallNotification(callName);
    console.log(notification);

    await fs.consumeNotification(notification[0]);
  }
}

test(process.argv[2] !== 'call');
