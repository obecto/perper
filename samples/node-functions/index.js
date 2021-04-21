const perper = require("perper");

// await perper({
//         function1: {
//             parameters: [Number, Number, Number],
//             action: function1
//         },
//         function2: {
//             parameters: [Number, Number, Number],
//             action: function2
//         }
//     }
// )

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
