const perper = require("perper");
const Stream = require("perper/model/Stream");
const generator = require("./generator/index");
const processor = require("./processor/index");
const consumer = require("./consumer/index");

async function main() {
  const context = await perper({
    generator: {
      parameters: Object,
      action: generator
    },
    processor: {
      parameters: Object,
      action: processor
    },
    consumer: {
      parameters: [Stream],
      action: consumer
    }
  });

  var generatorStream = await context.streamFunction("generator", { 0: 20 });
  var processorStream = await context.streamFunction(
    "processor",
    { 0: generatorStream, 1: 10 }
  );

  // var consumerAction = await context.streamAction("consumer", [ processorStream ]);
  // console.log(consumerAction);
}

main();