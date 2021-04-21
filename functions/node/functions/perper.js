const initializeIgnite = require("../utils/initializeIgnite");
const Serializer = require("../service/Serializer");
const PerperInstanceData = require("../cache/PerperInstanceData");

module.exports = async function perper(input, type, callback, mapArrayToParams = true) {
  const igniteClient = await initializeIgnite({ fabric_host: "127.0.0.1" });
  const perperInstance = new PerperInstanceData(igniteClient, new Serializer());
  const parameters = await perperInstance.setTriggerValue(input, type);

  if (parameters instanceof Array && mapArrayToParams) {
    callback.apply(this, parameters);
  } else {
    callback.call(this, parameters);
  }
};
