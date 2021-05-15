const EnumItem = require('apache-ignite-client').EnumItem;
const BinaryUtils = require('apache-ignite-client/lib/internal/BinaryUtils');

function createEnumItem (typeName, value) {
  const e = new EnumItem(BinaryUtils.hashCode(typeName));
  e.setOrdinal(value);
  return e;
}

module.exports = createEnumItem;
