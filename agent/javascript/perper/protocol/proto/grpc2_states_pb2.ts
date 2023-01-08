/* eslint-disable */
import { grpc } from "@improbable-eng/grpc-web";
import { BrowserHeaders } from "browser-headers";
import Long from "long";
import _m0 from "protobufjs/minimal";
import { Observable } from "rxjs";
import { share } from "rxjs/operators";
import { Any } from "./google/protobuf/any_pb2";
import { Empty } from "./google/protobuf/empty_pb2";
import { CacheOptions, PerperDictionary, PerperList } from "./grpc2_model_pb2";

export const protobufPackage = "perper.grpc2";

export interface StatesDictionaryCreateRequest {
  dictionary: PerperDictionary | undefined;
  cacheOptions: CacheOptions | undefined;
}

/**
 * StatesDictionaryOperateRequest operations guide:
 * Set/Put: set_new_value + new_value
 * SetIfNotChanged/Replace`3: set_new_value + new_value + compare_existing_value + expected_existing_value
 * SetIfNotExisting/PutIfAbsent: set_new_value + new_value + compare_existing_value
 * SetIfExisting/Replace`2: no matching operation
 * Remove: set_new_value
 * RemoveIfNotChanged/Remove`3: set_new_value + compare_existing_value + expected_existing_value
 * Get: get_existing_value
 * GetAndSet: get_existing_value + set_new_value + new_value
 * GetAnd...: get_existing_value + ...
 * ContainsKey: (nothing)
 */
export interface StatesDictionaryOperateRequest {
  dictionary: PerperDictionary | undefined;
  key: Any | undefined;
  getExistingValue: boolean;
  setNewValue: boolean;
  newValue: Any | undefined;
  compareExistingValue: boolean;
  expectedExistingValue: Any | undefined;
}

export interface StatesDictionaryOperateResponse {
  operationSuccessful: boolean;
  previousValue: Any | undefined;
}

export interface StatesDictionaryCountItemsRequest {
  dictionary: PerperDictionary | undefined;
}

export interface StatesDictionaryCountItemsResponse {
  count: number;
}

export interface StatesDictionaryListItemsRequest {
  dictionary: PerperDictionary | undefined;
}

export interface StatesDictionaryListItemsResponse {
  key: Any | undefined;
  value: Any | undefined;
}

export interface StatesDictionaryQuerySQLRequest {
  dictionary: PerperDictionary | undefined;
  sql: string;
}

export interface StatesDictionaryQuerySQLResponse {
  sqlResultValues: Any[];
}

export interface StatesDictionaryDeleteRequest {
  dictionary: PerperDictionary | undefined;
  keepCache: boolean;
}

export interface StatesListCreateRequest {
  list: PerperList | undefined;
  cacheOptions: CacheOptions | undefined;
}

/**
 * StatesListOperateRequest operations guide
 * Get: index/raw_index + get_values + values_count
 * PushFront: at_front + insert_values
 * PushBack: at_back + insert_values
 * PopFront: at_front + values_count + get_values + remove_values
 * PopBack: at_back + values_count + get_values + remove_values
 * Set: index/raw_index + values_count + remove_values + insert_values*1
 * Insert: index/raw_index + insert_values
 * Remove: index/raw_index + values_count + remove_values
 */
export interface StatesListOperateRequest {
  list: PerperList | undefined;
  atFront?: boolean | undefined;
  atBack?: boolean | undefined;
  index?: number | undefined;
  indexBackwards?: number | undefined;
  rawIndex?: number | undefined;
  rawIndexBackwards?: number | undefined;
  valuesCount: number;
  getValues: boolean;
  removeValues: boolean;
  insertValues: Any[];
}

export interface StatesListOperateResponse {
  values: Any[];
}

export interface StatesListLocateRequest {
  list: PerperList | undefined;
  value: Any | undefined;
}

export interface StatesListLocateResponse {
  found: boolean;
  index: number;
  rawIndex: number;
}

export interface StatesListCountItemsRequest {
  list: PerperList | undefined;
}

export interface StatesListCountItemsResponse {
  count: number;
}

export interface StatesListListItemsRequest {
  list: PerperList | undefined;
}

export interface StatesListListItemsResponse {
  index: number;
  rawIndex: number;
  value: Any | undefined;
}

export interface StatesListQuerySQLRequest {
  list: PerperList | undefined;
  sql: string;
}

export interface StatesListQuerySQLResponse {
  sqlResultValues: Any[];
}

export interface StatesListDeleteRequest {
  list: PerperList | undefined;
  keepCache: boolean;
}

function createBaseStatesDictionaryCreateRequest(): StatesDictionaryCreateRequest {
  return { dictionary: undefined, cacheOptions: undefined };
}

export const StatesDictionaryCreateRequest = {
  encode(message: StatesDictionaryCreateRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.dictionary !== undefined) {
      PerperDictionary.encode(message.dictionary, writer.uint32(10).fork()).ldelim();
    }
    if (message.cacheOptions !== undefined) {
      CacheOptions.encode(message.cacheOptions, writer.uint32(162).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryCreateRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryCreateRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.dictionary = PerperDictionary.decode(reader, reader.uint32());
          break;
        case 20:
          message.cacheOptions = CacheOptions.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryCreateRequest {
    return {
      dictionary: isSet(object.dictionary) ? PerperDictionary.fromJSON(object.dictionary) : undefined,
      cacheOptions: isSet(object.cacheOptions) ? CacheOptions.fromJSON(object.cacheOptions) : undefined,
    };
  },

  toJSON(message: StatesDictionaryCreateRequest): unknown {
    const obj: any = {};
    message.dictionary !== undefined &&
      (obj.dictionary = message.dictionary ? PerperDictionary.toJSON(message.dictionary) : undefined);
    message.cacheOptions !== undefined &&
      (obj.cacheOptions = message.cacheOptions ? CacheOptions.toJSON(message.cacheOptions) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryCreateRequest>, I>>(
    object: I,
  ): StatesDictionaryCreateRequest {
    const message = createBaseStatesDictionaryCreateRequest();
    message.dictionary = (object.dictionary !== undefined && object.dictionary !== null)
      ? PerperDictionary.fromPartial(object.dictionary)
      : undefined;
    message.cacheOptions = (object.cacheOptions !== undefined && object.cacheOptions !== null)
      ? CacheOptions.fromPartial(object.cacheOptions)
      : undefined;
    return message;
  },
};

function createBaseStatesDictionaryOperateRequest(): StatesDictionaryOperateRequest {
  return {
    dictionary: undefined,
    key: undefined,
    getExistingValue: false,
    setNewValue: false,
    newValue: undefined,
    compareExistingValue: false,
    expectedExistingValue: undefined,
  };
}

export const StatesDictionaryOperateRequest = {
  encode(message: StatesDictionaryOperateRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.dictionary !== undefined) {
      PerperDictionary.encode(message.dictionary, writer.uint32(10).fork()).ldelim();
    }
    if (message.key !== undefined) {
      Any.encode(message.key, writer.uint32(18).fork()).ldelim();
    }
    if (message.getExistingValue === true) {
      writer.uint32(24).bool(message.getExistingValue);
    }
    if (message.setNewValue === true) {
      writer.uint32(32).bool(message.setNewValue);
    }
    if (message.newValue !== undefined) {
      Any.encode(message.newValue, writer.uint32(42).fork()).ldelim();
    }
    if (message.compareExistingValue === true) {
      writer.uint32(48).bool(message.compareExistingValue);
    }
    if (message.expectedExistingValue !== undefined) {
      Any.encode(message.expectedExistingValue, writer.uint32(58).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryOperateRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryOperateRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.dictionary = PerperDictionary.decode(reader, reader.uint32());
          break;
        case 2:
          message.key = Any.decode(reader, reader.uint32());
          break;
        case 3:
          message.getExistingValue = reader.bool();
          break;
        case 4:
          message.setNewValue = reader.bool();
          break;
        case 5:
          message.newValue = Any.decode(reader, reader.uint32());
          break;
        case 6:
          message.compareExistingValue = reader.bool();
          break;
        case 7:
          message.expectedExistingValue = Any.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryOperateRequest {
    return {
      dictionary: isSet(object.dictionary) ? PerperDictionary.fromJSON(object.dictionary) : undefined,
      key: isSet(object.key) ? Any.fromJSON(object.key) : undefined,
      getExistingValue: isSet(object.getExistingValue) ? Boolean(object.getExistingValue) : false,
      setNewValue: isSet(object.setNewValue) ? Boolean(object.setNewValue) : false,
      newValue: isSet(object.newValue) ? Any.fromJSON(object.newValue) : undefined,
      compareExistingValue: isSet(object.compareExistingValue) ? Boolean(object.compareExistingValue) : false,
      expectedExistingValue: isSet(object.expectedExistingValue)
        ? Any.fromJSON(object.expectedExistingValue)
        : undefined,
    };
  },

  toJSON(message: StatesDictionaryOperateRequest): unknown {
    const obj: any = {};
    message.dictionary !== undefined &&
      (obj.dictionary = message.dictionary ? PerperDictionary.toJSON(message.dictionary) : undefined);
    message.key !== undefined && (obj.key = message.key ? Any.toJSON(message.key) : undefined);
    message.getExistingValue !== undefined && (obj.getExistingValue = message.getExistingValue);
    message.setNewValue !== undefined && (obj.setNewValue = message.setNewValue);
    message.newValue !== undefined && (obj.newValue = message.newValue ? Any.toJSON(message.newValue) : undefined);
    message.compareExistingValue !== undefined && (obj.compareExistingValue = message.compareExistingValue);
    message.expectedExistingValue !== undefined &&
      (obj.expectedExistingValue = message.expectedExistingValue
        ? Any.toJSON(message.expectedExistingValue)
        : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryOperateRequest>, I>>(
    object: I,
  ): StatesDictionaryOperateRequest {
    const message = createBaseStatesDictionaryOperateRequest();
    message.dictionary = (object.dictionary !== undefined && object.dictionary !== null)
      ? PerperDictionary.fromPartial(object.dictionary)
      : undefined;
    message.key = (object.key !== undefined && object.key !== null) ? Any.fromPartial(object.key) : undefined;
    message.getExistingValue = object.getExistingValue ?? false;
    message.setNewValue = object.setNewValue ?? false;
    message.newValue = (object.newValue !== undefined && object.newValue !== null)
      ? Any.fromPartial(object.newValue)
      : undefined;
    message.compareExistingValue = object.compareExistingValue ?? false;
    message.expectedExistingValue =
      (object.expectedExistingValue !== undefined && object.expectedExistingValue !== null)
        ? Any.fromPartial(object.expectedExistingValue)
        : undefined;
    return message;
  },
};

function createBaseStatesDictionaryOperateResponse(): StatesDictionaryOperateResponse {
  return { operationSuccessful: false, previousValue: undefined };
}

export const StatesDictionaryOperateResponse = {
  encode(message: StatesDictionaryOperateResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.operationSuccessful === true) {
      writer.uint32(8).bool(message.operationSuccessful);
    }
    if (message.previousValue !== undefined) {
      Any.encode(message.previousValue, writer.uint32(18).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryOperateResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryOperateResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.operationSuccessful = reader.bool();
          break;
        case 2:
          message.previousValue = Any.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryOperateResponse {
    return {
      operationSuccessful: isSet(object.operationSuccessful) ? Boolean(object.operationSuccessful) : false,
      previousValue: isSet(object.previousValue) ? Any.fromJSON(object.previousValue) : undefined,
    };
  },

  toJSON(message: StatesDictionaryOperateResponse): unknown {
    const obj: any = {};
    message.operationSuccessful !== undefined && (obj.operationSuccessful = message.operationSuccessful);
    message.previousValue !== undefined &&
      (obj.previousValue = message.previousValue ? Any.toJSON(message.previousValue) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryOperateResponse>, I>>(
    object: I,
  ): StatesDictionaryOperateResponse {
    const message = createBaseStatesDictionaryOperateResponse();
    message.operationSuccessful = object.operationSuccessful ?? false;
    message.previousValue = (object.previousValue !== undefined && object.previousValue !== null)
      ? Any.fromPartial(object.previousValue)
      : undefined;
    return message;
  },
};

function createBaseStatesDictionaryCountItemsRequest(): StatesDictionaryCountItemsRequest {
  return { dictionary: undefined };
}

export const StatesDictionaryCountItemsRequest = {
  encode(message: StatesDictionaryCountItemsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.dictionary !== undefined) {
      PerperDictionary.encode(message.dictionary, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryCountItemsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryCountItemsRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.dictionary = PerperDictionary.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryCountItemsRequest {
    return { dictionary: isSet(object.dictionary) ? PerperDictionary.fromJSON(object.dictionary) : undefined };
  },

  toJSON(message: StatesDictionaryCountItemsRequest): unknown {
    const obj: any = {};
    message.dictionary !== undefined &&
      (obj.dictionary = message.dictionary ? PerperDictionary.toJSON(message.dictionary) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryCountItemsRequest>, I>>(
    object: I,
  ): StatesDictionaryCountItemsRequest {
    const message = createBaseStatesDictionaryCountItemsRequest();
    message.dictionary = (object.dictionary !== undefined && object.dictionary !== null)
      ? PerperDictionary.fromPartial(object.dictionary)
      : undefined;
    return message;
  },
};

function createBaseStatesDictionaryCountItemsResponse(): StatesDictionaryCountItemsResponse {
  return { count: 0 };
}

export const StatesDictionaryCountItemsResponse = {
  encode(message: StatesDictionaryCountItemsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.count !== 0) {
      writer.uint32(8).int32(message.count);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryCountItemsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryCountItemsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.count = reader.int32();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryCountItemsResponse {
    return { count: isSet(object.count) ? Number(object.count) : 0 };
  },

  toJSON(message: StatesDictionaryCountItemsResponse): unknown {
    const obj: any = {};
    message.count !== undefined && (obj.count = Math.round(message.count));
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryCountItemsResponse>, I>>(
    object: I,
  ): StatesDictionaryCountItemsResponse {
    const message = createBaseStatesDictionaryCountItemsResponse();
    message.count = object.count ?? 0;
    return message;
  },
};

function createBaseStatesDictionaryListItemsRequest(): StatesDictionaryListItemsRequest {
  return { dictionary: undefined };
}

export const StatesDictionaryListItemsRequest = {
  encode(message: StatesDictionaryListItemsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.dictionary !== undefined) {
      PerperDictionary.encode(message.dictionary, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryListItemsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryListItemsRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.dictionary = PerperDictionary.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryListItemsRequest {
    return { dictionary: isSet(object.dictionary) ? PerperDictionary.fromJSON(object.dictionary) : undefined };
  },

  toJSON(message: StatesDictionaryListItemsRequest): unknown {
    const obj: any = {};
    message.dictionary !== undefined &&
      (obj.dictionary = message.dictionary ? PerperDictionary.toJSON(message.dictionary) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryListItemsRequest>, I>>(
    object: I,
  ): StatesDictionaryListItemsRequest {
    const message = createBaseStatesDictionaryListItemsRequest();
    message.dictionary = (object.dictionary !== undefined && object.dictionary !== null)
      ? PerperDictionary.fromPartial(object.dictionary)
      : undefined;
    return message;
  },
};

function createBaseStatesDictionaryListItemsResponse(): StatesDictionaryListItemsResponse {
  return { key: undefined, value: undefined };
}

export const StatesDictionaryListItemsResponse = {
  encode(message: StatesDictionaryListItemsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.key !== undefined) {
      Any.encode(message.key, writer.uint32(10).fork()).ldelim();
    }
    if (message.value !== undefined) {
      Any.encode(message.value, writer.uint32(18).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryListItemsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryListItemsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.key = Any.decode(reader, reader.uint32());
          break;
        case 2:
          message.value = Any.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryListItemsResponse {
    return {
      key: isSet(object.key) ? Any.fromJSON(object.key) : undefined,
      value: isSet(object.value) ? Any.fromJSON(object.value) : undefined,
    };
  },

  toJSON(message: StatesDictionaryListItemsResponse): unknown {
    const obj: any = {};
    message.key !== undefined && (obj.key = message.key ? Any.toJSON(message.key) : undefined);
    message.value !== undefined && (obj.value = message.value ? Any.toJSON(message.value) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryListItemsResponse>, I>>(
    object: I,
  ): StatesDictionaryListItemsResponse {
    const message = createBaseStatesDictionaryListItemsResponse();
    message.key = (object.key !== undefined && object.key !== null) ? Any.fromPartial(object.key) : undefined;
    message.value = (object.value !== undefined && object.value !== null) ? Any.fromPartial(object.value) : undefined;
    return message;
  },
};

function createBaseStatesDictionaryQuerySQLRequest(): StatesDictionaryQuerySQLRequest {
  return { dictionary: undefined, sql: "" };
}

export const StatesDictionaryQuerySQLRequest = {
  encode(message: StatesDictionaryQuerySQLRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.dictionary !== undefined) {
      PerperDictionary.encode(message.dictionary, writer.uint32(10).fork()).ldelim();
    }
    if (message.sql !== "") {
      writer.uint32(18).string(message.sql);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryQuerySQLRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryQuerySQLRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.dictionary = PerperDictionary.decode(reader, reader.uint32());
          break;
        case 2:
          message.sql = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryQuerySQLRequest {
    return {
      dictionary: isSet(object.dictionary) ? PerperDictionary.fromJSON(object.dictionary) : undefined,
      sql: isSet(object.sql) ? String(object.sql) : "",
    };
  },

  toJSON(message: StatesDictionaryQuerySQLRequest): unknown {
    const obj: any = {};
    message.dictionary !== undefined &&
      (obj.dictionary = message.dictionary ? PerperDictionary.toJSON(message.dictionary) : undefined);
    message.sql !== undefined && (obj.sql = message.sql);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryQuerySQLRequest>, I>>(
    object: I,
  ): StatesDictionaryQuerySQLRequest {
    const message = createBaseStatesDictionaryQuerySQLRequest();
    message.dictionary = (object.dictionary !== undefined && object.dictionary !== null)
      ? PerperDictionary.fromPartial(object.dictionary)
      : undefined;
    message.sql = object.sql ?? "";
    return message;
  },
};

function createBaseStatesDictionaryQuerySQLResponse(): StatesDictionaryQuerySQLResponse {
  return { sqlResultValues: [] };
}

export const StatesDictionaryQuerySQLResponse = {
  encode(message: StatesDictionaryQuerySQLResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    for (const v of message.sqlResultValues) {
      Any.encode(v!, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryQuerySQLResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryQuerySQLResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.sqlResultValues.push(Any.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryQuerySQLResponse {
    return {
      sqlResultValues: Array.isArray(object?.sqlResultValues)
        ? object.sqlResultValues.map((e: any) => Any.fromJSON(e))
        : [],
    };
  },

  toJSON(message: StatesDictionaryQuerySQLResponse): unknown {
    const obj: any = {};
    if (message.sqlResultValues) {
      obj.sqlResultValues = message.sqlResultValues.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.sqlResultValues = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryQuerySQLResponse>, I>>(
    object: I,
  ): StatesDictionaryQuerySQLResponse {
    const message = createBaseStatesDictionaryQuerySQLResponse();
    message.sqlResultValues = object.sqlResultValues?.map((e) => Any.fromPartial(e)) || [];
    return message;
  },
};

function createBaseStatesDictionaryDeleteRequest(): StatesDictionaryDeleteRequest {
  return { dictionary: undefined, keepCache: false };
}

export const StatesDictionaryDeleteRequest = {
  encode(message: StatesDictionaryDeleteRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.dictionary !== undefined) {
      PerperDictionary.encode(message.dictionary, writer.uint32(10).fork()).ldelim();
    }
    if (message.keepCache === true) {
      writer.uint32(16).bool(message.keepCache);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesDictionaryDeleteRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesDictionaryDeleteRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.dictionary = PerperDictionary.decode(reader, reader.uint32());
          break;
        case 2:
          message.keepCache = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesDictionaryDeleteRequest {
    return {
      dictionary: isSet(object.dictionary) ? PerperDictionary.fromJSON(object.dictionary) : undefined,
      keepCache: isSet(object.keepCache) ? Boolean(object.keepCache) : false,
    };
  },

  toJSON(message: StatesDictionaryDeleteRequest): unknown {
    const obj: any = {};
    message.dictionary !== undefined &&
      (obj.dictionary = message.dictionary ? PerperDictionary.toJSON(message.dictionary) : undefined);
    message.keepCache !== undefined && (obj.keepCache = message.keepCache);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesDictionaryDeleteRequest>, I>>(
    object: I,
  ): StatesDictionaryDeleteRequest {
    const message = createBaseStatesDictionaryDeleteRequest();
    message.dictionary = (object.dictionary !== undefined && object.dictionary !== null)
      ? PerperDictionary.fromPartial(object.dictionary)
      : undefined;
    message.keepCache = object.keepCache ?? false;
    return message;
  },
};

function createBaseStatesListCreateRequest(): StatesListCreateRequest {
  return { list: undefined, cacheOptions: undefined };
}

export const StatesListCreateRequest = {
  encode(message: StatesListCreateRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.list !== undefined) {
      PerperList.encode(message.list, writer.uint32(10).fork()).ldelim();
    }
    if (message.cacheOptions !== undefined) {
      CacheOptions.encode(message.cacheOptions, writer.uint32(162).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListCreateRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListCreateRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.list = PerperList.decode(reader, reader.uint32());
          break;
        case 20:
          message.cacheOptions = CacheOptions.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListCreateRequest {
    return {
      list: isSet(object.list) ? PerperList.fromJSON(object.list) : undefined,
      cacheOptions: isSet(object.cacheOptions) ? CacheOptions.fromJSON(object.cacheOptions) : undefined,
    };
  },

  toJSON(message: StatesListCreateRequest): unknown {
    const obj: any = {};
    message.list !== undefined && (obj.list = message.list ? PerperList.toJSON(message.list) : undefined);
    message.cacheOptions !== undefined &&
      (obj.cacheOptions = message.cacheOptions ? CacheOptions.toJSON(message.cacheOptions) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListCreateRequest>, I>>(object: I): StatesListCreateRequest {
    const message = createBaseStatesListCreateRequest();
    message.list = (object.list !== undefined && object.list !== null)
      ? PerperList.fromPartial(object.list)
      : undefined;
    message.cacheOptions = (object.cacheOptions !== undefined && object.cacheOptions !== null)
      ? CacheOptions.fromPartial(object.cacheOptions)
      : undefined;
    return message;
  },
};

function createBaseStatesListOperateRequest(): StatesListOperateRequest {
  return {
    list: undefined,
    atFront: undefined,
    atBack: undefined,
    index: undefined,
    indexBackwards: undefined,
    rawIndex: undefined,
    rawIndexBackwards: undefined,
    valuesCount: 0,
    getValues: false,
    removeValues: false,
    insertValues: [],
  };
}

export const StatesListOperateRequest = {
  encode(message: StatesListOperateRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.list !== undefined) {
      PerperList.encode(message.list, writer.uint32(10).fork()).ldelim();
    }
    if (message.atFront !== undefined) {
      writer.uint32(16).bool(message.atFront);
    }
    if (message.atBack !== undefined) {
      writer.uint32(24).bool(message.atBack);
    }
    if (message.index !== undefined) {
      writer.uint32(32).uint32(message.index);
    }
    if (message.indexBackwards !== undefined) {
      writer.uint32(40).uint32(message.indexBackwards);
    }
    if (message.rawIndex !== undefined) {
      writer.uint32(80).int64(message.rawIndex);
    }
    if (message.rawIndexBackwards !== undefined) {
      writer.uint32(88).int64(message.rawIndexBackwards);
    }
    if (message.valuesCount !== 0) {
      writer.uint32(48).uint64(message.valuesCount);
    }
    if (message.getValues === true) {
      writer.uint32(56).bool(message.getValues);
    }
    if (message.removeValues === true) {
      writer.uint32(64).bool(message.removeValues);
    }
    for (const v of message.insertValues) {
      Any.encode(v!, writer.uint32(74).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListOperateRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListOperateRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.list = PerperList.decode(reader, reader.uint32());
          break;
        case 2:
          message.atFront = reader.bool();
          break;
        case 3:
          message.atBack = reader.bool();
          break;
        case 4:
          message.index = reader.uint32();
          break;
        case 5:
          message.indexBackwards = reader.uint32();
          break;
        case 10:
          message.rawIndex = longToNumber(reader.int64() as Long);
          break;
        case 11:
          message.rawIndexBackwards = longToNumber(reader.int64() as Long);
          break;
        case 6:
          message.valuesCount = longToNumber(reader.uint64() as Long);
          break;
        case 7:
          message.getValues = reader.bool();
          break;
        case 8:
          message.removeValues = reader.bool();
          break;
        case 9:
          message.insertValues.push(Any.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListOperateRequest {
    return {
      list: isSet(object.list) ? PerperList.fromJSON(object.list) : undefined,
      atFront: isSet(object.atFront) ? Boolean(object.atFront) : undefined,
      atBack: isSet(object.atBack) ? Boolean(object.atBack) : undefined,
      index: isSet(object.index) ? Number(object.index) : undefined,
      indexBackwards: isSet(object.indexBackwards) ? Number(object.indexBackwards) : undefined,
      rawIndex: isSet(object.rawIndex) ? Number(object.rawIndex) : undefined,
      rawIndexBackwards: isSet(object.rawIndexBackwards) ? Number(object.rawIndexBackwards) : undefined,
      valuesCount: isSet(object.valuesCount) ? Number(object.valuesCount) : 0,
      getValues: isSet(object.getValues) ? Boolean(object.getValues) : false,
      removeValues: isSet(object.removeValues) ? Boolean(object.removeValues) : false,
      insertValues: Array.isArray(object?.insertValues) ? object.insertValues.map((e: any) => Any.fromJSON(e)) : [],
    };
  },

  toJSON(message: StatesListOperateRequest): unknown {
    const obj: any = {};
    message.list !== undefined && (obj.list = message.list ? PerperList.toJSON(message.list) : undefined);
    message.atFront !== undefined && (obj.atFront = message.atFront);
    message.atBack !== undefined && (obj.atBack = message.atBack);
    message.index !== undefined && (obj.index = Math.round(message.index));
    message.indexBackwards !== undefined && (obj.indexBackwards = Math.round(message.indexBackwards));
    message.rawIndex !== undefined && (obj.rawIndex = Math.round(message.rawIndex));
    message.rawIndexBackwards !== undefined && (obj.rawIndexBackwards = Math.round(message.rawIndexBackwards));
    message.valuesCount !== undefined && (obj.valuesCount = Math.round(message.valuesCount));
    message.getValues !== undefined && (obj.getValues = message.getValues);
    message.removeValues !== undefined && (obj.removeValues = message.removeValues);
    if (message.insertValues) {
      obj.insertValues = message.insertValues.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.insertValues = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListOperateRequest>, I>>(object: I): StatesListOperateRequest {
    const message = createBaseStatesListOperateRequest();
    message.list = (object.list !== undefined && object.list !== null)
      ? PerperList.fromPartial(object.list)
      : undefined;
    message.atFront = object.atFront ?? undefined;
    message.atBack = object.atBack ?? undefined;
    message.index = object.index ?? undefined;
    message.indexBackwards = object.indexBackwards ?? undefined;
    message.rawIndex = object.rawIndex ?? undefined;
    message.rawIndexBackwards = object.rawIndexBackwards ?? undefined;
    message.valuesCount = object.valuesCount ?? 0;
    message.getValues = object.getValues ?? false;
    message.removeValues = object.removeValues ?? false;
    message.insertValues = object.insertValues?.map((e) => Any.fromPartial(e)) || [];
    return message;
  },
};

function createBaseStatesListOperateResponse(): StatesListOperateResponse {
  return { values: [] };
}

export const StatesListOperateResponse = {
  encode(message: StatesListOperateResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    for (const v of message.values) {
      Any.encode(v!, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListOperateResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListOperateResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.values.push(Any.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListOperateResponse {
    return { values: Array.isArray(object?.values) ? object.values.map((e: any) => Any.fromJSON(e)) : [] };
  },

  toJSON(message: StatesListOperateResponse): unknown {
    const obj: any = {};
    if (message.values) {
      obj.values = message.values.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.values = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListOperateResponse>, I>>(object: I): StatesListOperateResponse {
    const message = createBaseStatesListOperateResponse();
    message.values = object.values?.map((e) => Any.fromPartial(e)) || [];
    return message;
  },
};

function createBaseStatesListLocateRequest(): StatesListLocateRequest {
  return { list: undefined, value: undefined };
}

export const StatesListLocateRequest = {
  encode(message: StatesListLocateRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.list !== undefined) {
      PerperList.encode(message.list, writer.uint32(10).fork()).ldelim();
    }
    if (message.value !== undefined) {
      Any.encode(message.value, writer.uint32(66).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListLocateRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListLocateRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.list = PerperList.decode(reader, reader.uint32());
          break;
        case 8:
          message.value = Any.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListLocateRequest {
    return {
      list: isSet(object.list) ? PerperList.fromJSON(object.list) : undefined,
      value: isSet(object.value) ? Any.fromJSON(object.value) : undefined,
    };
  },

  toJSON(message: StatesListLocateRequest): unknown {
    const obj: any = {};
    message.list !== undefined && (obj.list = message.list ? PerperList.toJSON(message.list) : undefined);
    message.value !== undefined && (obj.value = message.value ? Any.toJSON(message.value) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListLocateRequest>, I>>(object: I): StatesListLocateRequest {
    const message = createBaseStatesListLocateRequest();
    message.list = (object.list !== undefined && object.list !== null)
      ? PerperList.fromPartial(object.list)
      : undefined;
    message.value = (object.value !== undefined && object.value !== null) ? Any.fromPartial(object.value) : undefined;
    return message;
  },
};

function createBaseStatesListLocateResponse(): StatesListLocateResponse {
  return { found: false, index: 0, rawIndex: 0 };
}

export const StatesListLocateResponse = {
  encode(message: StatesListLocateResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.found === true) {
      writer.uint32(8).bool(message.found);
    }
    if (message.index !== 0) {
      writer.uint32(16).uint32(message.index);
    }
    if (message.rawIndex !== 0) {
      writer.uint32(24).int64(message.rawIndex);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListLocateResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListLocateResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.found = reader.bool();
          break;
        case 2:
          message.index = reader.uint32();
          break;
        case 3:
          message.rawIndex = longToNumber(reader.int64() as Long);
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListLocateResponse {
    return {
      found: isSet(object.found) ? Boolean(object.found) : false,
      index: isSet(object.index) ? Number(object.index) : 0,
      rawIndex: isSet(object.rawIndex) ? Number(object.rawIndex) : 0,
    };
  },

  toJSON(message: StatesListLocateResponse): unknown {
    const obj: any = {};
    message.found !== undefined && (obj.found = message.found);
    message.index !== undefined && (obj.index = Math.round(message.index));
    message.rawIndex !== undefined && (obj.rawIndex = Math.round(message.rawIndex));
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListLocateResponse>, I>>(object: I): StatesListLocateResponse {
    const message = createBaseStatesListLocateResponse();
    message.found = object.found ?? false;
    message.index = object.index ?? 0;
    message.rawIndex = object.rawIndex ?? 0;
    return message;
  },
};

function createBaseStatesListCountItemsRequest(): StatesListCountItemsRequest {
  return { list: undefined };
}

export const StatesListCountItemsRequest = {
  encode(message: StatesListCountItemsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.list !== undefined) {
      PerperList.encode(message.list, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListCountItemsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListCountItemsRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.list = PerperList.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListCountItemsRequest {
    return { list: isSet(object.list) ? PerperList.fromJSON(object.list) : undefined };
  },

  toJSON(message: StatesListCountItemsRequest): unknown {
    const obj: any = {};
    message.list !== undefined && (obj.list = message.list ? PerperList.toJSON(message.list) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListCountItemsRequest>, I>>(object: I): StatesListCountItemsRequest {
    const message = createBaseStatesListCountItemsRequest();
    message.list = (object.list !== undefined && object.list !== null)
      ? PerperList.fromPartial(object.list)
      : undefined;
    return message;
  },
};

function createBaseStatesListCountItemsResponse(): StatesListCountItemsResponse {
  return { count: 0 };
}

export const StatesListCountItemsResponse = {
  encode(message: StatesListCountItemsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.count !== 0) {
      writer.uint32(8).uint32(message.count);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListCountItemsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListCountItemsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.count = reader.uint32();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListCountItemsResponse {
    return { count: isSet(object.count) ? Number(object.count) : 0 };
  },

  toJSON(message: StatesListCountItemsResponse): unknown {
    const obj: any = {};
    message.count !== undefined && (obj.count = Math.round(message.count));
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListCountItemsResponse>, I>>(object: I): StatesListCountItemsResponse {
    const message = createBaseStatesListCountItemsResponse();
    message.count = object.count ?? 0;
    return message;
  },
};

function createBaseStatesListListItemsRequest(): StatesListListItemsRequest {
  return { list: undefined };
}

export const StatesListListItemsRequest = {
  encode(message: StatesListListItemsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.list !== undefined) {
      PerperList.encode(message.list, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListListItemsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListListItemsRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.list = PerperList.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListListItemsRequest {
    return { list: isSet(object.list) ? PerperList.fromJSON(object.list) : undefined };
  },

  toJSON(message: StatesListListItemsRequest): unknown {
    const obj: any = {};
    message.list !== undefined && (obj.list = message.list ? PerperList.toJSON(message.list) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListListItemsRequest>, I>>(object: I): StatesListListItemsRequest {
    const message = createBaseStatesListListItemsRequest();
    message.list = (object.list !== undefined && object.list !== null)
      ? PerperList.fromPartial(object.list)
      : undefined;
    return message;
  },
};

function createBaseStatesListListItemsResponse(): StatesListListItemsResponse {
  return { index: 0, rawIndex: 0, value: undefined };
}

export const StatesListListItemsResponse = {
  encode(message: StatesListListItemsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.index !== 0) {
      writer.uint32(8).uint32(message.index);
    }
    if (message.rawIndex !== 0) {
      writer.uint32(24).int64(message.rawIndex);
    }
    if (message.value !== undefined) {
      Any.encode(message.value, writer.uint32(18).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListListItemsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListListItemsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.index = reader.uint32();
          break;
        case 3:
          message.rawIndex = longToNumber(reader.int64() as Long);
          break;
        case 2:
          message.value = Any.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListListItemsResponse {
    return {
      index: isSet(object.index) ? Number(object.index) : 0,
      rawIndex: isSet(object.rawIndex) ? Number(object.rawIndex) : 0,
      value: isSet(object.value) ? Any.fromJSON(object.value) : undefined,
    };
  },

  toJSON(message: StatesListListItemsResponse): unknown {
    const obj: any = {};
    message.index !== undefined && (obj.index = Math.round(message.index));
    message.rawIndex !== undefined && (obj.rawIndex = Math.round(message.rawIndex));
    message.value !== undefined && (obj.value = message.value ? Any.toJSON(message.value) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListListItemsResponse>, I>>(object: I): StatesListListItemsResponse {
    const message = createBaseStatesListListItemsResponse();
    message.index = object.index ?? 0;
    message.rawIndex = object.rawIndex ?? 0;
    message.value = (object.value !== undefined && object.value !== null) ? Any.fromPartial(object.value) : undefined;
    return message;
  },
};

function createBaseStatesListQuerySQLRequest(): StatesListQuerySQLRequest {
  return { list: undefined, sql: "" };
}

export const StatesListQuerySQLRequest = {
  encode(message: StatesListQuerySQLRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.list !== undefined) {
      PerperList.encode(message.list, writer.uint32(10).fork()).ldelim();
    }
    if (message.sql !== "") {
      writer.uint32(18).string(message.sql);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListQuerySQLRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListQuerySQLRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.list = PerperList.decode(reader, reader.uint32());
          break;
        case 2:
          message.sql = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListQuerySQLRequest {
    return {
      list: isSet(object.list) ? PerperList.fromJSON(object.list) : undefined,
      sql: isSet(object.sql) ? String(object.sql) : "",
    };
  },

  toJSON(message: StatesListQuerySQLRequest): unknown {
    const obj: any = {};
    message.list !== undefined && (obj.list = message.list ? PerperList.toJSON(message.list) : undefined);
    message.sql !== undefined && (obj.sql = message.sql);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListQuerySQLRequest>, I>>(object: I): StatesListQuerySQLRequest {
    const message = createBaseStatesListQuerySQLRequest();
    message.list = (object.list !== undefined && object.list !== null)
      ? PerperList.fromPartial(object.list)
      : undefined;
    message.sql = object.sql ?? "";
    return message;
  },
};

function createBaseStatesListQuerySQLResponse(): StatesListQuerySQLResponse {
  return { sqlResultValues: [] };
}

export const StatesListQuerySQLResponse = {
  encode(message: StatesListQuerySQLResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    for (const v of message.sqlResultValues) {
      Any.encode(v!, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListQuerySQLResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListQuerySQLResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.sqlResultValues.push(Any.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListQuerySQLResponse {
    return {
      sqlResultValues: Array.isArray(object?.sqlResultValues)
        ? object.sqlResultValues.map((e: any) => Any.fromJSON(e))
        : [],
    };
  },

  toJSON(message: StatesListQuerySQLResponse): unknown {
    const obj: any = {};
    if (message.sqlResultValues) {
      obj.sqlResultValues = message.sqlResultValues.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.sqlResultValues = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListQuerySQLResponse>, I>>(object: I): StatesListQuerySQLResponse {
    const message = createBaseStatesListQuerySQLResponse();
    message.sqlResultValues = object.sqlResultValues?.map((e) => Any.fromPartial(e)) || [];
    return message;
  },
};

function createBaseStatesListDeleteRequest(): StatesListDeleteRequest {
  return { list: undefined, keepCache: false };
}

export const StatesListDeleteRequest = {
  encode(message: StatesListDeleteRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.list !== undefined) {
      PerperList.encode(message.list, writer.uint32(10).fork()).ldelim();
    }
    if (message.keepCache === true) {
      writer.uint32(16).bool(message.keepCache);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StatesListDeleteRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStatesListDeleteRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.list = PerperList.decode(reader, reader.uint32());
          break;
        case 2:
          message.keepCache = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StatesListDeleteRequest {
    return {
      list: isSet(object.list) ? PerperList.fromJSON(object.list) : undefined,
      keepCache: isSet(object.keepCache) ? Boolean(object.keepCache) : false,
    };
  },

  toJSON(message: StatesListDeleteRequest): unknown {
    const obj: any = {};
    message.list !== undefined && (obj.list = message.list ? PerperList.toJSON(message.list) : undefined);
    message.keepCache !== undefined && (obj.keepCache = message.keepCache);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StatesListDeleteRequest>, I>>(object: I): StatesListDeleteRequest {
    const message = createBaseStatesListDeleteRequest();
    message.list = (object.list !== undefined && object.list !== null)
      ? PerperList.fromPartial(object.list)
      : undefined;
    message.keepCache = object.keepCache ?? false;
    return message;
  },
};

export interface FabricStatesDictionary {
  Create(request: DeepPartial<StatesDictionaryCreateRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  Operate(
    request: DeepPartial<StatesDictionaryOperateRequest>,
    metadata?: grpc.Metadata,
  ): Promise<StatesDictionaryOperateResponse>;
  ListItems(
    request: DeepPartial<StatesDictionaryListItemsRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StatesDictionaryListItemsResponse>;
  CountItems(
    request: DeepPartial<StatesDictionaryCountItemsRequest>,
    metadata?: grpc.Metadata,
  ): Promise<StatesDictionaryCountItemsResponse>;
  QuerySQL(
    request: DeepPartial<StatesDictionaryQuerySQLRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StatesDictionaryQuerySQLResponse>;
  Delete(request: DeepPartial<StatesDictionaryDeleteRequest>, metadata?: grpc.Metadata): Promise<Empty>;
}

export class FabricStatesDictionaryClientImpl implements FabricStatesDictionary {
  private readonly rpc: Rpc;

  constructor(rpc: Rpc) {
    this.rpc = rpc;
    this.Create = this.Create.bind(this);
    this.Operate = this.Operate.bind(this);
    this.ListItems = this.ListItems.bind(this);
    this.CountItems = this.CountItems.bind(this);
    this.QuerySQL = this.QuerySQL.bind(this);
    this.Delete = this.Delete.bind(this);
  }

  Create(request: DeepPartial<StatesDictionaryCreateRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(
      FabricStatesDictionaryCreateDesc,
      StatesDictionaryCreateRequest.fromPartial(request),
      metadata,
    );
  }

  Operate(
    request: DeepPartial<StatesDictionaryOperateRequest>,
    metadata?: grpc.Metadata,
  ): Promise<StatesDictionaryOperateResponse> {
    return this.rpc.unary(
      FabricStatesDictionaryOperateDesc,
      StatesDictionaryOperateRequest.fromPartial(request),
      metadata,
    );
  }

  ListItems(
    request: DeepPartial<StatesDictionaryListItemsRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StatesDictionaryListItemsResponse> {
    return this.rpc.invoke(
      FabricStatesDictionaryListItemsDesc,
      StatesDictionaryListItemsRequest.fromPartial(request),
      metadata,
    );
  }

  CountItems(
    request: DeepPartial<StatesDictionaryCountItemsRequest>,
    metadata?: grpc.Metadata,
  ): Promise<StatesDictionaryCountItemsResponse> {
    return this.rpc.unary(
      FabricStatesDictionaryCountItemsDesc,
      StatesDictionaryCountItemsRequest.fromPartial(request),
      metadata,
    );
  }

  QuerySQL(
    request: DeepPartial<StatesDictionaryQuerySQLRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StatesDictionaryQuerySQLResponse> {
    return this.rpc.invoke(
      FabricStatesDictionaryQuerySQLDesc,
      StatesDictionaryQuerySQLRequest.fromPartial(request),
      metadata,
    );
  }

  Delete(request: DeepPartial<StatesDictionaryDeleteRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(
      FabricStatesDictionaryDeleteDesc,
      StatesDictionaryDeleteRequest.fromPartial(request),
      metadata,
    );
  }
}

export const FabricStatesDictionaryDesc = { serviceName: "perper.grpc2.FabricStatesDictionary" };

export const FabricStatesDictionaryCreateDesc: UnaryMethodDefinitionish = {
  methodName: "Create",
  service: FabricStatesDictionaryDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesDictionaryCreateRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = Empty.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesDictionaryOperateDesc: UnaryMethodDefinitionish = {
  methodName: "Operate",
  service: FabricStatesDictionaryDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesDictionaryOperateRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesDictionaryOperateResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesDictionaryListItemsDesc: UnaryMethodDefinitionish = {
  methodName: "ListItems",
  service: FabricStatesDictionaryDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return StatesDictionaryListItemsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesDictionaryListItemsResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesDictionaryCountItemsDesc: UnaryMethodDefinitionish = {
  methodName: "CountItems",
  service: FabricStatesDictionaryDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesDictionaryCountItemsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesDictionaryCountItemsResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesDictionaryQuerySQLDesc: UnaryMethodDefinitionish = {
  methodName: "QuerySQL",
  service: FabricStatesDictionaryDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return StatesDictionaryQuerySQLRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesDictionaryQuerySQLResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesDictionaryDeleteDesc: UnaryMethodDefinitionish = {
  methodName: "Delete",
  service: FabricStatesDictionaryDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesDictionaryDeleteRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = Empty.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export interface FabricStatesList {
  Create(request: DeepPartial<StatesListCreateRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  Operate(request: DeepPartial<StatesListOperateRequest>, metadata?: grpc.Metadata): Promise<StatesListOperateResponse>;
  Locate(request: DeepPartial<StatesListLocateRequest>, metadata?: grpc.Metadata): Promise<StatesListLocateResponse>;
  ListItems(
    request: DeepPartial<StatesListListItemsRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StatesListListItemsResponse>;
  CountItems(
    request: DeepPartial<StatesListCountItemsRequest>,
    metadata?: grpc.Metadata,
  ): Promise<StatesListCountItemsResponse>;
  QuerySQL(
    request: DeepPartial<StatesListQuerySQLRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StatesListQuerySQLResponse>;
  Delete(request: DeepPartial<StatesListDeleteRequest>, metadata?: grpc.Metadata): Promise<Empty>;
}

export class FabricStatesListClientImpl implements FabricStatesList {
  private readonly rpc: Rpc;

  constructor(rpc: Rpc) {
    this.rpc = rpc;
    this.Create = this.Create.bind(this);
    this.Operate = this.Operate.bind(this);
    this.Locate = this.Locate.bind(this);
    this.ListItems = this.ListItems.bind(this);
    this.CountItems = this.CountItems.bind(this);
    this.QuerySQL = this.QuerySQL.bind(this);
    this.Delete = this.Delete.bind(this);
  }

  Create(request: DeepPartial<StatesListCreateRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricStatesListCreateDesc, StatesListCreateRequest.fromPartial(request), metadata);
  }

  Operate(
    request: DeepPartial<StatesListOperateRequest>,
    metadata?: grpc.Metadata,
  ): Promise<StatesListOperateResponse> {
    return this.rpc.unary(FabricStatesListOperateDesc, StatesListOperateRequest.fromPartial(request), metadata);
  }

  Locate(request: DeepPartial<StatesListLocateRequest>, metadata?: grpc.Metadata): Promise<StatesListLocateResponse> {
    return this.rpc.unary(FabricStatesListLocateDesc, StatesListLocateRequest.fromPartial(request), metadata);
  }

  ListItems(
    request: DeepPartial<StatesListListItemsRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StatesListListItemsResponse> {
    return this.rpc.invoke(FabricStatesListListItemsDesc, StatesListListItemsRequest.fromPartial(request), metadata);
  }

  CountItems(
    request: DeepPartial<StatesListCountItemsRequest>,
    metadata?: grpc.Metadata,
  ): Promise<StatesListCountItemsResponse> {
    return this.rpc.unary(FabricStatesListCountItemsDesc, StatesListCountItemsRequest.fromPartial(request), metadata);
  }

  QuerySQL(
    request: DeepPartial<StatesListQuerySQLRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StatesListQuerySQLResponse> {
    return this.rpc.invoke(FabricStatesListQuerySQLDesc, StatesListQuerySQLRequest.fromPartial(request), metadata);
  }

  Delete(request: DeepPartial<StatesListDeleteRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricStatesListDeleteDesc, StatesListDeleteRequest.fromPartial(request), metadata);
  }
}

export const FabricStatesListDesc = { serviceName: "perper.grpc2.FabricStatesList" };

export const FabricStatesListCreateDesc: UnaryMethodDefinitionish = {
  methodName: "Create",
  service: FabricStatesListDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesListCreateRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = Empty.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesListOperateDesc: UnaryMethodDefinitionish = {
  methodName: "Operate",
  service: FabricStatesListDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesListOperateRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesListOperateResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesListLocateDesc: UnaryMethodDefinitionish = {
  methodName: "Locate",
  service: FabricStatesListDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesListLocateRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesListLocateResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesListListItemsDesc: UnaryMethodDefinitionish = {
  methodName: "ListItems",
  service: FabricStatesListDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return StatesListListItemsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesListListItemsResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesListCountItemsDesc: UnaryMethodDefinitionish = {
  methodName: "CountItems",
  service: FabricStatesListDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesListCountItemsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesListCountItemsResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesListQuerySQLDesc: UnaryMethodDefinitionish = {
  methodName: "QuerySQL",
  service: FabricStatesListDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return StatesListQuerySQLRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StatesListQuerySQLResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStatesListDeleteDesc: UnaryMethodDefinitionish = {
  methodName: "Delete",
  service: FabricStatesListDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StatesListDeleteRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = Empty.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

interface UnaryMethodDefinitionishR extends grpc.UnaryMethodDefinition<any, any> {
  requestStream: any;
  responseStream: any;
}

type UnaryMethodDefinitionish = UnaryMethodDefinitionishR;

interface Rpc {
  unary<T extends UnaryMethodDefinitionish>(
    methodDesc: T,
    request: any,
    metadata: grpc.Metadata | undefined,
  ): Promise<any>;
  invoke<T extends UnaryMethodDefinitionish>(
    methodDesc: T,
    request: any,
    metadata: grpc.Metadata | undefined,
  ): Observable<any>;
}

export class GrpcWebImpl {
  private host: string;
  private options: {
    transport?: grpc.TransportFactory;
    streamingTransport?: grpc.TransportFactory;
    debug?: boolean;
    metadata?: grpc.Metadata;
    upStreamRetryCodes?: number[];
  };

  constructor(
    host: string,
    options: {
      transport?: grpc.TransportFactory;
      streamingTransport?: grpc.TransportFactory;
      debug?: boolean;
      metadata?: grpc.Metadata;
      upStreamRetryCodes?: number[];
    },
  ) {
    this.host = host;
    this.options = options;
  }

  unary<T extends UnaryMethodDefinitionish>(
    methodDesc: T,
    _request: any,
    metadata: grpc.Metadata | undefined,
  ): Promise<any> {
    const request = { ..._request, ...methodDesc.requestType };
    const maybeCombinedMetadata = metadata && this.options.metadata
      ? new BrowserHeaders({ ...this.options?.metadata.headersMap, ...metadata?.headersMap })
      : metadata || this.options.metadata;
    return new Promise((resolve, reject) => {
      grpc.unary(methodDesc, {
        request,
        host: this.host,
        metadata: maybeCombinedMetadata,
        transport: this.options.transport,
        debug: this.options.debug,
        onEnd: function (response) {
          if (response.status === grpc.Code.OK) {
            resolve(response.message!.toObject());
          } else {
            const err = new GrpcWebError(response.statusMessage, response.status, response.trailers);
            reject(err);
          }
        },
      });
    });
  }

  invoke<T extends UnaryMethodDefinitionish>(
    methodDesc: T,
    _request: any,
    metadata: grpc.Metadata | undefined,
  ): Observable<any> {
    const upStreamCodes = this.options.upStreamRetryCodes || [];
    const DEFAULT_TIMEOUT_TIME: number = 3_000;
    const request = { ..._request, ...methodDesc.requestType };
    const maybeCombinedMetadata = metadata && this.options.metadata
      ? new BrowserHeaders({ ...this.options?.metadata.headersMap, ...metadata?.headersMap })
      : metadata || this.options.metadata;
    return new Observable((observer) => {
      const upStream = (() => {
        const client = grpc.invoke(methodDesc, {
          host: this.host,
          request,
          transport: this.options.streamingTransport || this.options.transport,
          metadata: maybeCombinedMetadata,
          debug: this.options.debug,
          onMessage: (next) => observer.next(next),
          onEnd: (code: grpc.Code, message: string, trailers: grpc.Metadata) => {
            if (code === 0) {
              observer.complete();
            } else if (upStreamCodes.includes(code)) {
              setTimeout(upStream, DEFAULT_TIMEOUT_TIME);
            } else {
              const err = new Error(message) as any;
              err.code = code;
              err.metadata = trailers;
              observer.error(err);
            }
          },
        });
        observer.add(() => client.close());
      });
      upStream();
    }).pipe(share());
  }
}

declare var self: any | undefined;
declare var window: any | undefined;
declare var global: any | undefined;
var tsProtoGlobalThis: any = (() => {
  if (typeof globalThis !== "undefined") {
    return globalThis;
  }
  if (typeof self !== "undefined") {
    return self;
  }
  if (typeof window !== "undefined") {
    return window;
  }
  if (typeof global !== "undefined") {
    return global;
  }
  throw "Unable to locate global object";
})();

type Builtin = Date | Function | Uint8Array | string | number | boolean | undefined;

export type DeepPartial<T> = T extends Builtin ? T
  : T extends Array<infer U> ? Array<DeepPartial<U>> : T extends ReadonlyArray<infer U> ? ReadonlyArray<DeepPartial<U>>
  : T extends {} ? { [K in keyof T]?: DeepPartial<T[K]> }
  : Partial<T>;

type KeysOfUnion<T> = T extends T ? keyof T : never;
export type Exact<P, I extends P> = P extends Builtin ? P
  : P & { [K in keyof P]: Exact<P[K], I[K]> } & { [K in Exclude<keyof I, KeysOfUnion<P>>]: never };

function longToNumber(long: Long): number {
  if (long.gt(Number.MAX_SAFE_INTEGER)) {
    throw new tsProtoGlobalThis.Error("Value is larger than Number.MAX_SAFE_INTEGER");
  }
  return long.toNumber();
}

if (_m0.util.Long !== Long) {
  _m0.util.Long = Long as any;
  _m0.configure();
}

function isSet(value: any): boolean {
  return value !== null && value !== undefined;
}

export class GrpcWebError extends tsProtoGlobalThis.Error {
  constructor(message: string, public code: grpc.Code, public metadata: grpc.Metadata) {
    super(message);
  }
}
