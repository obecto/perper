const perper = require("perper");

module.exports = async function(context, input) {
  const expectedMap = new Map();
  expectedMap.set(0, Boolean);
  expectedMap.set(1, String);

  await perper(
    { fabric_host: "127.0.0.1" },
    input,
    [Boolean, String, String, Number, Number, expectedMap],
    async function(a, b, c, d, e, f) {
      console.log(f);
    }
  );
};
