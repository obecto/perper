const perper = require("perper");

module.exports = async function(context, input) {
  await perper(
    { fabric_host: "127.0.0.1" },
    input,
    [Boolean, String, String, Number],
    async function(a, b, c, d) {
      context.log([a, b, c, d]);
      context.log(typeof a);
      context.log(typeof b);
      context.log(typeof c);
      context.log(typeof d);
    }
  );
};
