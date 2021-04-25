const perper = require('perper');
const perperFunction = require('./PerperFunction/index');
const perperFunction2 = require('./PerperFunction2/index');

const expectedMap = new Map();
expectedMap.set(0, Boolean);
expectedMap.set(1, String);

const pTypes = [
  Boolean,
  String,
  String,
  Number,
  Number,
  Map,
  expectedMap,
  [String, Boolean, String]
];

perper({
  PerperFunction: {
    parameters: pTypes,
    action: perperFunction
  },
  PerperFunction2: {
    parameters: pTypes,
    action: perperFunction2,
    mapArrayToParams: false
  }
});
