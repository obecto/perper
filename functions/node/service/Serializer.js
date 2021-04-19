function Serializer () {}

Serializer.prototype.serialize = function (data) {
  return data;
};

Serializer.prototype.deserialize = function (data, type) {
  const dataType = typeof data;
  const dataConstructor = data.constructor;

  if (data === null || data === undefined) return null;
  if (type === null || type === undefined) return data;

  if (['string', 'boolean', 'number'].includes(dataType)) {
    if (dataConstructor === type) {
      return data;
    } else if (type === String) {
      let res = data.toString();
      console.debug('Primitive type conversion applied: Converted ' + dataConstructor.name + ' to String - "' + res + '".');
      return res;
    } else if (type === Boolean) {
      let res = (typeof data === 'number' && data > 0) || (typeof data === 'string' && data.toLowerCase() === 'true');
      console.debug('Primitive type conversion applied: Converted ' + dataConstructor.name + ' "' + data + '" to ' + res + ' of type Boolean.');
      return res;
    } else if (type === Number && !isNaN(parseFloat(data))) {
      let res = parseFloat(data);
      console.debug('Primitive type conversion applied: Converted ' + dataConstructor.name + ' "' + data + '" to ' + res + ' of type Number.');
      return res;
    } else {
      throw new Error('Primitive type mismatch: Cannot convert ' + dataConstructor.name + ' to ' + type + '.');
    }
  }

  if (data instanceof Array) {
    if (!(type instanceof Array)) throw new Error('Got collection data incompatible with the provided type.');
    var isTuple = (data.length === type.length + 1) && (data[data.length - 1] === type.length);
    if (data.length === type.length || isTuple) {
      if (isTuple) {
        console.debug('Handling a tuple. If this is a mistake please check out the length of your types and the incoming data.');
        console.debug('Data:');
        console.debug(data);
        data.pop();
      }

      let res = Array(data.length);
      for (let i = 0; i < data.length; i++) {
        res[i] = this.deserialize(data[i], type[i]);
      }

      return res;
    } else {
      console.debug('Data:');
      console.debug(data);
      throw new Error('Arrays length mismatch: Provided ' + type.length + ' types but got data with length ' + data.length);
    }
  };

  if (type === Map) { // Provided a Map class as a type.
    if (data instanceof Map) return data;
    if (typeof data === 'object') {
      console.debug('Attempting object to Map conversion...');
      return new Map(Object.entries(data));
    }

    throw new Error('Cannot deserialize ' + typeof data + ' to Map.')
  } else if (data instanceof Map) { // Provided a Map instance with key/value subtypes.
    if (!(type instanceof Map)) throw new Error('Got map data incompatible with the provided type.');    
    let res = new Map();
    for (let [key, value] of data.entries()) {
      res.set(key, this.deserialize(value, type.get(key)));
    }

    return res;
  }

  console.debug('Data:');
  console.debug(data);
  throw new Error('Could not handle deserialization. Unexpected combination of required types and incoming data.');
};

module.exports = Serializer;
