const perper = require('perper');
const perperFunction = require('./PerperFunction/index');

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
    action: (params) => {
      console.log(params);
    },
    mapArrayToParams: false
  }
});
