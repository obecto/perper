const IgniteClient = require('apache-ignite-client');
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;
const ObjectType = IgniteClient.ObjectType;
const ScanQuery = IgniteClient.ScanQuery;
const ComplexObjectType = IgniteClient.ComplexObjectType;

function timeout (ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function asyncReadWrite (isStatic) {
  const igniteClient = new IgniteClient();
  try {
    await igniteClient.connect(
      new IgniteClientConfiguration('127.0.0.1:10800')
    );

    const cacheNames = await igniteClient.cacheNames();
    const streamName = cacheNames.find(name =>
      name.includes(isStatic ? 'SimpleDataStream' : 'DynamicDataStream')
    );

    if (streamName) {
      const simpleDataStream = await igniteClient.getOrCreateCache(streamName);
      const streamSize = await simpleDataStream.getSize();

      simpleDataStream.setKeyType(ObjectType.PRIMITIVE_TYPE.LONG);
      if (isStatic) {
        const compType = new ComplexObjectType(
          { json: '', priority: 0, name: '' },
          'SimpleData'
        );
        compType.setFieldType('json', ObjectType.PRIMITIVE_TYPE.STRING);
        compType.setFieldType('name', ObjectType.PRIMITIVE_TYPE.STRING);
        compType.setFieldType('priority', ObjectType.PRIMITIVE_TYPE.INTEGER);
        simpleDataStream.setValueType(compType);
      } else {
        const compType = new ComplexObjectType({}, 'DynamicData');
        simpleDataStream.setValueType(compType);
      }

      if (isStatic) {
        await simpleDataStream.put(streamSize + 1, {
          json: "{ 'test' : 0 }",
          name: 'Radi Cho',
          priority: 100
        });
      }

      const scanQuery = new ScanQuery().setPageSize(streamSize);
      const cursor = await simpleDataStream.query(scanQuery);
      const caches = await cursor.getAll();
      for (const cacheEntry of caches) {
        console.log(cacheEntry._value);
        await timeout(20);
      }

      console.log('Finished');
    } else {
      console.log('Perper stream not started yet');
    }
  } catch (err) {
    console.log(err.message);
  } finally {
    igniteClient.disconnect();
  }
}

asyncReadWrite(false); // dynamic
