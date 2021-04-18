const initializeIgnite = require('perper/service/initializeIgnite');
const Serializer = require('perper/service/Serializer');
const PerperInstanceData = require('perper/cache/PerperInstanceData');

module.exports = async function (context, input) {
  await perper(input, [String, String, String, Number], async function(a, b, c, d) {
    console.log(a);
    console.log(b);
    console.log(c);
    console.log(d);
  });
};

async function perper (input, type, callback) {
  const igniteClient = await initializeIgnite({ fabric_host: '127.0.0.1' });
  const perperInstance = new PerperInstanceData(igniteClient, new Serializer());
  const parameters = await perperInstance.setTriggerValue(input, type);
  console.log(parameters);
}
