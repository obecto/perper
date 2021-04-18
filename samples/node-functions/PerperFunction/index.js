const initializeIgnite = require('perper/service/initializeIgnite');
const Serializer = require('perper/service/Serializer');
const PerperInstanceData = require('perper/cache/PerperInstanceData');

module.exports = async function (context, input) {
  await perper(input);
};

async function perper (input, types, callback) {
  const igniteClient = await initializeIgnite({ fabric_host: '127.0.0.1' });
  const perperInstance = new PerperInstanceData(igniteClient, new Serializer());
  perperInstance.setTriggerValue(input);
}
