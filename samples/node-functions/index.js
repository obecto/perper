const perper = require("perper");

const expectedMap = new Map();
expectedMap.set(0, Boolean);
expectedMap.set(1, String);

perper({
  Application1: {
    parameters: [
      Boolean,
      String,
      String,
      Number,
      Number,
      Map,
      expectedMap,
      [String, Boolean, String]
    ],
    action: async function(a, b, c, d, e, f, g, h) {
      console.log([a, b, c, d, e, f, g, h]);
    }
  }
});
