const perper = require("perper");

module.exports = async function(context, input) {
  const expectedMap = new Map();
  expectedMap.set(0, Boolean);
  expectedMap.set(1, String);

  await perper(
    input,
    [Boolean, String, String, Number, Number, Map, expectedMap, [String, Boolean, String]],
    async function(a, b, c, d, e, f, g, h) {
      console.log([a, b, c, d, e, f, g, h]);
    },
  );
};
