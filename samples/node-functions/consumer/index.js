module.exports = async function (stream) {
  console.log('Consumer!');
  console.log(await this.fs.consumeItem('streams', stream.StreamName));
  console.log('Consumed!');
};
