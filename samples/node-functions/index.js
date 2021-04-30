const perper = require("perper");
const Stream = require("perper/model/Stream");
const generator = require("./generator/index");
const processor = require("./processor/index");
const consumer = require("./consumer/index");

async function main() {
  const context = await perper({
    generator: {
      parameters: [Number],
      action: generator
    },
    processor: {
      parameters: [Stream, Number],
      action: processor
    },
    consumer: {
      parameters: [Stream],
      action: consumer
    }
  });

  var generatorStream = context.streamFunction("generator", 20);
  // var processorStream = context.streamFunction(
  //   "processor",
  //   generatorStream,
  //   10
  // );

  // context.streamAction("consumer", processorStream);
}

main();