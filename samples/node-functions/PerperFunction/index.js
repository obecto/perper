const perper = require("perper");

module.exports = async function(context, input) {
  await perper(
    { fabric_host: "127.0.0.1" },
    input,
    [Boolean, String, String, Number, Number, null],
    async function(a, b, c, d, e, f) {
      console.log(f);
    }
  );
};
