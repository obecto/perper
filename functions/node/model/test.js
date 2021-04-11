const IgniteClient = require('apache-ignite-client');
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;

const FabricService = require('../services/FabricService');
const Stream = require('./Stream');

async function test (streamName) {
  const config = { fabric_host: '127.0.0.1' };
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ':10800')
  );

  const fs = new FabricService(igniteClient, config);

  const stream1 = new Stream();
  stream1.setParameters(streamName, igniteClient, fs);
  const enumerable = stream1.getEnumerable(
    new Map(),
    false,
    false
  );

  enumerable.run();

  // const context = new Context({}, {}, {}, {});
  // console.log(context.startAgent("test", {}));

  // console.log(enumerable);
}

test(process.argv[2]);
