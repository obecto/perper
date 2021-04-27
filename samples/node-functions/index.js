const perper = require("perper");
const Stream = require("perper/model/Stream");
const generator = require("./generator/index");
const processor = require("./processor/index");
const consumer = require("./consumer/index");

perper({
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

// var generatorStream = perper.context.streamFunction("generator", 20);
// var processorStream = perper.context.streamFunction(
//   "processor",
//   generatorStream,
//   10
// );

// perper.context.streamAction("consumer", processorStream);
