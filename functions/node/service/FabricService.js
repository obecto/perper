const process = require('process');
const path = require('path');
const PROTO_PATH = __dirname + '../../../../proto/fabric.proto';
const grpc = require('@grpc/grpc-js');
// const Schema = require("./fabric_pb");
const protoLoader = require('@grpc/proto-loader');
const IgniteClient = require('apache-ignite-client');
const ComplexObjectType = IgniteClient.ComplexObjectType;
const CacheConfiguration = IgniteClient.CacheConfiguration;
const CacheKeyConfiguration = IgniteClient.CacheKeyConfiguration;
const ObjectType = IgniteClient.ObjectType;
const ObjectArrayType = IgniteClient.ObjectArrayType;

// Monkey patch for Ignite hashing
// https://issues.apache.org/jira/browse/IGNITE-14369
const BinaryUtils = require('apache-ignite-client/lib/internal/BinaryUtils');
const BinaryType = require('apache-ignite-client/lib/internal/BinaryType');
const BinaryCommunicator = require('apache-ignite-client/lib/internal/BinaryCommunicator');

const HEADER_LENGTH = 24;
BinaryUtils.contentHashCode = function (buffer, startPos, endPos) {
  let hash = 1;
  for (let i = startPos; i <= endPos + startPos - HEADER_LENGTH; i++) {
    hash = 31 * hash + buffer._buffer.readInt8(i);
    hash |= 0; // Convert to 32bit integer
  }
  return hash;
};

// Monkey patch for Ignite affinity keys
const affinityKeyFieldNames = {
  NotificationKeyString: 'affinity',
  NotificationKeyLong: 'affinity'
};

BinaryType.prototype._write = async function (buffer) {
  // type id
  buffer.writeInteger(this._id);
  // type name
  BinaryCommunicator.writeString(buffer, this._name);
  // affinity key field name
  BinaryCommunicator.writeString(
    buffer,
    affinityKeyFieldNames[this._name] || null
  );
  // fields count
  buffer.writeInteger(this._fields.size);
  // fields
  for (const field of this._fields.values()) {
    await field._write(buffer);
  }
  await this._writeEnum(buffer);
  // schemas count
  buffer.writeInteger(this._schemas.size);
  for (const schema of this._schemas.values()) {
    await schema._write(buffer);
  }
};

// Patch: Support reading longs as string
const oldReadTypedObject = BinaryCommunicator.prototype._readTypedObject;
BinaryCommunicator.prototype._readTypedObject = function (
  buffer,
  objectTypeCode,
  expectedType = null
) {
  if (objectTypeCode === BinaryUtils.TYPE_CODE.LONG) { return buffer.readLong().toString(); }
  return oldReadTypedObject.call(this, buffer, objectTypeCode, expectedType);
};

const oldCheckStandardTypeCompatibility =
  BinaryUtils.checkStandardTypeCompatibility;

// Patch: Support writing longs as string;
// Patch: Fix calling _isSet when undefined;
BinaryUtils.checkStandardTypeCompatibility = function (
  value,
  typeCode,
  type = null
) {
  if (typeCode === BinaryUtils.TYPE_CODE.LONG) return;
  if (typeCode === BinaryUtils.TYPE_CODE.COLLECTION) {
    // if (!(type && value instanceof Set && type._isSet && type._isSet() || value instanceof Array)) {
    //   throw Errors.IgniteClientError.typeCastError(valueType, type && type._isSet() ? 'set' : typeCode);
    // }
    return;
  }

  return oldCheckStandardTypeCompatibility.call(this, value, typeCode, type);
};

const packageDefinition = protoLoader.loadSync(PROTO_PATH, {
  keepCase: true,
  longs: String,
  enums: String,
  defaults: true,
  oneofs: true
});

/**
 * @class {FabricService} FabricService
 * @param {IgniteClient} ignite The Ignite thin client
 * @param {Object} config Configuration
 * @param {String} config.fabric_host Host
 */
function FabricService (ignite, config, overrideAgent) {
  this.ignite = ignite;
  console.log('Initializing Node Perper Fabric Service...');

  this.agentDelegate = process.env.PERPER_AGENT_NAME;
  if (!this.agentDelegate) this.agentDelegate = this.getAgentDelegateFromPath();
  if (overrideAgent) this.agentDelegate = overrideAgent;

  let rootAgent = process.env.PERPER_ROOT_AGENT;
  if (!rootAgent) rootAgent = '';

  this.isInitialAgent = rootAgent === this.agentDelegate;
  const cacheConfig = new CacheConfiguration();
  cacheConfig.setKeyConfigurations(
    new CacheKeyConfiguration('NotificationKey', 'affinity')
  );

  this.notificationsCache = this.ignite.getOrCreateCache(
    this.agentDelegate + '-$notifications',
    cacheConfig
  );

  const address = (config.fabric_host || 'localhost') + ':40400';

  this.grpcChannel = grpc.credentials.createInsecure();
  const routeguide = grpc.loadPackageDefinition(packageDefinition);
  this.grpcStub = new routeguide.perper.Fabric(
    address,
    grpc.credentials.createInsecure()
  );

  this.notificationFilter = { agentDelegate: this.agentDelegate };
}

FabricService.prototype.getAgentDelegateFromPath = function () {
  return path.basename(process.cwd());
};

/**
 * @param {function(any)} callback
 * @param {function(Error)} errorCallback
 * @param {function(any)} statusListener
 * @param {function()} endCallback
 */
FabricService.prototype.getNotifications = function (
  callback,
  errorCallback,
  statusListener,
  endCallback
) {
  const call = this.grpcStub.notifications({
    agentDelegate: this.agentDelegate
  });

  // https://bit.ly/2Q0DyL5
  call.on('data', async resp => {
    const notification = await this.generateNotification(resp);
    callback(notification);
  });

  call.on('error', function (e) {
    if (errorCallback) {
      errorCallback(e);
    } else {
      console.log('An error has occurred and the stream has been closed.');
      console.error(e);
    }
  });
  call.on('status', function (status) {
    if (statusListener) {
      statusListener(status);
    } else {
      console.log(status);
    }
  });
  call.on('end', function () {
    if (endCallback) {
      endCallback();
    } else {
      console.log('The server has finished sending');
    }
  });
};

function configureNotificationKey (nc, key) {
  const notKey = new ComplexObjectType(
    { affinity: '', key: 0 },
    key.affinity === 'stringAffinity'
      ? 'NotificationKeyString'
      : 'NotificationKeyLong'
  );

  const notVal = new ComplexObjectType({});

  if (key.affinity === 'stringAffinity') {
    notKey.setFieldType('affinity', ObjectType.PRIMITIVE_TYPE.STRING);
  } else {
    notKey.setFieldType('affinity', ObjectType.PRIMITIVE_TYPE.LONG);
  }
  notKey.setFieldType('key', ObjectType.PRIMITIVE_TYPE.LONG);

  nc.setKeyType(notKey);
  nc.setValueType(notVal);

  return {
    affinity: key.stringAffinity || key.intAffinity,
    key: key.key
  };
}

/**
 * @param {Object} key Notification key object
 * @param {Number} key.key Numeric notification ID
 * @param {String} key.affinity Key affinity
 * @param {String=} key.stringAffinity The string affinity value
 * @param {Number=} key.intAffinity The int affinity value
 */
FabricService.prototype.getCacheItem = async function (key) {
  return new Promise((resolve, reject) => {
    this.notificationsCache
      .then(async nc => {
        const incomingKey = configureNotificationKey(nc, key);
        const incomingNotification = await nc.get(incomingKey);
        resolve(incomingNotification);
      })
      .catch(e => reject(e));
  });
};

/**
 * @param {Object} key Notification key object
 * @param {Number} key.key Numeric notification ID
 * @param {String} key.affinity Key affinity
 * @param {String=} key.stringAffinity The string affinity value
 * @param {Number=} key.intAffinity The int affinity value
 */
FabricService.prototype.consumeNotification = function (notification, log = true) {
  return this.notificationsCache.then(async nc => {
    const incomingKey = configureNotificationKey(nc, notification[0]);
    await nc.getAndRemove(incomingKey);

    if (log) {
      console.log('Consumed notification: ');
      console.log(incomingKey);
    }
  });
};

/**
 * @param {Object} notificationData
 */
FabricService.prototype.generateNotification = async function (
  notificationData
) {
  const key = {
    key: notificationData.notificationKey,
    affinity: notificationData.affinity
  };

  if (notificationData.stringAffinity) { key.stringAffinity = notificationData.stringAffinity; }
  if (notificationData.intAffinity) { key.intAffinity = notificationData.intAffinity; }

  const item = await this.getCacheItem(key);
  return [key, item];
};

/**
 * @param {String} call Call name
 */
FabricService.prototype.getCallNotification = function (call) {
  return new Promise((resolve, reject) => {
    this.grpcStub.callResultNotification(
      { agentDelegate: this.agentDelegate, callName: call },
      async (err, responseMessage) => {
        if (err) {
          reject(err);
        } else {
          resolve(await this.generateNotification(responseMessage));
        }
      }
    );
  });
};

FabricService.prototype.generateCallDataType = function () {
  const compType = new ComplexObjectType(
    {
      Agent: "",
      AgentDelegate: "",
      Delegate: "",
      CallerAgentDelegate: "",
      Caller: "",
      Finished: true,
      LocalToData: true,
      Error: "",
      Parameters: null
    },
    "CallData"
  );

  compType.setFieldType("Agent", ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType("AgentDelegate", ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType("Delegate", ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType(
    "CallerAgentDelegate",
    ObjectType.PRIMITIVE_TYPE.STRING
  );
  compType.setFieldType("Caller", ObjectType.PRIMITIVE_TYPE.STRING);
  compType.setFieldType("Finished", ObjectType.PRIMITIVE_TYPE.BOOLEAN);
  compType.setFieldType("LocalToData", ObjectType.PRIMITIVE_TYPE.BOOLEAN);
  compType.setFieldType("Error", new ComplexObjectType({})); // FIXME: is actually nullable string
  compType.setFieldType("Parameters", new ObjectArrayType());

  return compType;
}

module.exports = FabricService;
