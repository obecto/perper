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

  var generatorStream = await context.streamFunction("generator", [20]);
  var processorStream = await context.streamFunction(
    "processor",
    [ generatorStream, 10 ]
  );

  var consumerAction = await context.streamAction("consumer", [ processorStream ]);
  console.log(consumerAction);
}

main();