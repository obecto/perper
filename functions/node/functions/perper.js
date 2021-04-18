const initializeIgnite = require("../utils/initializeIgnite");
const Serializer = require("../service/Serializer");
const PerperInstanceData = require("../cache/PerperInstanceData");

module.exports = async function perper(igniteConfig, input, type, callback) {
  const igniteClient = await initializeIgnite(igniteConfig);
  const perperInstance = new PerperInstanceData(igniteClient, new Serializer());
  const parameters = await perperInstance.setTriggerValue(input, type);

  if (parameters instanceof Array) {
    callback.apply(this, parameters);
  } else {
    callback();
  }
};
