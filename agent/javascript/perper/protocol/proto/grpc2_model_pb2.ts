/* eslint-disable */
import Long from "long";
import _m0 from "protobufjs/minimal";

export const protobufPackage = "perper.grpc2_model";

export interface PerperInstance {
  instance: string;
  agent: string;
}

export interface PerperStream {
  stream: string;
  startKey: number;
  stride: number;
}

export interface PerperExecution {
  execution: string;
}

export interface PerperDictionary {
  dictionary: string;
}

export interface PerperList {
  list: string;
}

export interface PerperError {
  message: string;
}

export interface CacheOptions {
  indexTypeUrls: string[];
  dataRegion: string;
}

function createBasePerperInstance(): PerperInstance {
  return { instance: "", agent: "" };
}

export const PerperInstance = {
  encode(message: PerperInstance, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.instance !== "") {
      writer.uint32(10).string(message.instance);
    }
    if (message.agent !== "") {
      writer.uint32(18).string(message.agent);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): PerperInstance {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBasePerperInstance();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.instance = reader.string();
          break;
        case 2:
          message.agent = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): PerperInstance {
    return {
      instance: isSet(object.instance) ? String(object.instance) : "",
      agent: isSet(object.agent) ? String(object.agent) : "",
    };
  },

  toJSON(message: PerperInstance): unknown {
    const obj: any = {};
    message.instance !== undefined && (obj.instance = message.instance);
    message.agent !== undefined && (obj.agent = message.agent);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<PerperInstance>, I>>(object: I): PerperInstance {
    const message = createBasePerperInstance();
    message.instance = object.instance ?? "";
    message.agent = object.agent ?? "";
    return message;
  },
};

function createBasePerperStream(): PerperStream {
  return { stream: "", startKey: 0, stride: 0 };
}

export const PerperStream = {
  encode(message: PerperStream, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== "") {
      writer.uint32(10).string(message.stream);
    }
    if (message.startKey !== 0) {
      writer.uint32(16).int64(message.startKey);
    }
    if (message.stride !== 0) {
      writer.uint32(24).int64(message.stride);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): PerperStream {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBasePerperStream();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.stream = reader.string();
          break;
        case 2:
          message.startKey = longToNumber(reader.int64() as Long);
          break;
        case 3:
          message.stride = longToNumber(reader.int64() as Long);
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): PerperStream {
    return {
      stream: isSet(object.stream) ? String(object.stream) : "",
      startKey: isSet(object.startKey) ? Number(object.startKey) : 0,
      stride: isSet(object.stride) ? Number(object.stride) : 0,
    };
  },

  toJSON(message: PerperStream): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream);
    message.startKey !== undefined && (obj.startKey = Math.round(message.startKey));
    message.stride !== undefined && (obj.stride = Math.round(message.stride));
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<PerperStream>, I>>(object: I): PerperStream {
    const message = createBasePerperStream();
    message.stream = object.stream ?? "";
    message.startKey = object.startKey ?? 0;
    message.stride = object.stride ?? 0;
    return message;
  },
};

function createBasePerperExecution(): PerperExecution {
  return { execution: "" };
}

export const PerperExecution = {
  encode(message: PerperExecution, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== "") {
      writer.uint32(10).string(message.execution);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): PerperExecution {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBasePerperExecution();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.execution = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): PerperExecution {
    return { execution: isSet(object.execution) ? String(object.execution) : "" };
  },

  toJSON(message: PerperExecution): unknown {
    const obj: any = {};
    message.execution !== undefined && (obj.execution = message.execution);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<PerperExecution>, I>>(object: I): PerperExecution {
    const message = createBasePerperExecution();
    message.execution = object.execution ?? "";
    return message;
  },
};

function createBasePerperDictionary(): PerperDictionary {
  return { dictionary: "" };
}

export const PerperDictionary = {
  encode(message: PerperDictionary, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.dictionary !== "") {
      writer.uint32(10).string(message.dictionary);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): PerperDictionary {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBasePerperDictionary();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.dictionary = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): PerperDictionary {
    return { dictionary: isSet(object.dictionary) ? String(object.dictionary) : "" };
  },

  toJSON(message: PerperDictionary): unknown {
    const obj: any = {};
    message.dictionary !== undefined && (obj.dictionary = message.dictionary);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<PerperDictionary>, I>>(object: I): PerperDictionary {
    const message = createBasePerperDictionary();
    message.dictionary = object.dictionary ?? "";
    return message;
  },
};

function createBasePerperList(): PerperList {
  return { list: "" };
}

export const PerperList = {
  encode(message: PerperList, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.list !== "") {
      writer.uint32(10).string(message.list);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): PerperList {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBasePerperList();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.list = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): PerperList {
    return { list: isSet(object.list) ? String(object.list) : "" };
  },

  toJSON(message: PerperList): unknown {
    const obj: any = {};
    message.list !== undefined && (obj.list = message.list);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<PerperList>, I>>(object: I): PerperList {
    const message = createBasePerperList();
    message.list = object.list ?? "";
    return message;
  },
};

function createBasePerperError(): PerperError {
  return { message: "" };
}

export const PerperError = {
  encode(message: PerperError, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.message !== "") {
      writer.uint32(10).string(message.message);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): PerperError {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBasePerperError();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.message = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): PerperError {
    return { message: isSet(object.message) ? String(object.message) : "" };
  },

  toJSON(message: PerperError): unknown {
    const obj: any = {};
    message.message !== undefined && (obj.message = message.message);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<PerperError>, I>>(object: I): PerperError {
    const message = createBasePerperError();
    message.message = object.message ?? "";
    return message;
  },
};

function createBaseCacheOptions(): CacheOptions {
  return { indexTypeUrls: [], dataRegion: "" };
}

export const CacheOptions = {
  encode(message: CacheOptions, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    for (const v of message.indexTypeUrls) {
      writer.uint32(10).string(v!);
    }
    if (message.dataRegion !== "") {
      writer.uint32(18).string(message.dataRegion);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): CacheOptions {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseCacheOptions();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.indexTypeUrls.push(reader.string());
          break;
        case 2:
          message.dataRegion = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): CacheOptions {
    return {
      indexTypeUrls: Array.isArray(object?.indexTypeUrls) ? object.indexTypeUrls.map((e: any) => String(e)) : [],
      dataRegion: isSet(object.dataRegion) ? String(object.dataRegion) : "",
    };
  },

  toJSON(message: CacheOptions): unknown {
    const obj: any = {};
    if (message.indexTypeUrls) {
      obj.indexTypeUrls = message.indexTypeUrls.map((e) => e);
    } else {
      obj.indexTypeUrls = [];
    }
    message.dataRegion !== undefined && (obj.dataRegion = message.dataRegion);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<CacheOptions>, I>>(object: I): CacheOptions {
    const message = createBaseCacheOptions();
    message.indexTypeUrls = object.indexTypeUrls?.map((e) => e) || [];
    message.dataRegion = object.dataRegion ?? "";
    return message;
  },
};

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
