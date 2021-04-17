const IgniteClient = require("apache-ignite-client");
const IgniteClientConfiguration = IgniteClient.IgniteClientConfiguration;

module.exports = async function(config) {
  const igniteClient = new IgniteClient();
  await igniteClient.connect(
    new IgniteClientConfiguration(config.fabric_host + ":10800")
  );

  return igniteClient;
};
