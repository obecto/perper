const perper = require('perper');
const perperFunction = require('./PerperFunction/index');
const perperFunction2 = require('./PerperFunction2/index');

const expectedMap = new Map();
expectedMap.set(0, Boolean);
expectedMap.set(1, String);

perper({
  PerperFunction: {
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
    action: perperFunction
  },
  PerperFunction2: {
    parameters: [String, String, String],
    action: perperFunction2,
    mapArrayToParams: false
  }
});
