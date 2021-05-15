const perper = require('perper');
const Stream = require('perper/model/Stream');
const generator = require('./generator/index');
const processor = require('./processor/index');
const consumer = require('./consumer/index');

async function main () {
  const context = await perper({
    generator: {
      parameters: [Number],
      action: generator
    },
    processor: {
      parameters: [Stream],
      action: processor
    },
    consumer: {
      parameters: [Stream],
      action: consumer
    }
  });

  const generatorStream = await context.streamFunction('generator', [20]);
  const processorStream = await context.streamFunction(
    'processor',
    [generatorStream]
  );

  const consumerAction = await context.streamAction('consumer', [processorStream]);
  console.log(consumerAction);
}

main();
