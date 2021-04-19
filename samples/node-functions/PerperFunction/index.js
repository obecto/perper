const perper = require("perper");

module.exports = async function(context, input) {
  const expectedMap = new Map();
  expectedMap.set(0, Boolean);
  expectedMap.set(1, String);

  await perper(
    input,
    [Boolean, String, String, Number, Number, expectedMap],
    async function(a, b, c, d, e, f) {
      console.log([a, b, c, d, e, f]);
    },
    {}, // igniteConfig - optional | defaults to {}
    false // mapArrayToParams - optional | defaults to true
  );
};
