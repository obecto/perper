/* eslint-disable */
import { grpc } from "@improbable-eng/grpc-web";
import { BrowserHeaders } from "browser-headers";
import Long from "long";
import _m0 from "protobufjs/minimal";
import { Observable } from "rxjs";
import { share } from "rxjs/operators";
import { Any } from "./google/protobuf/any_pb2";
import { Empty } from "./google/protobuf/empty_pb2";
import { CacheOptions, PerperStream } from "./grpc2_model_pb2";

export const protobufPackage = "perper.grpc2";

export interface StreamsCreateRequest {
  stream: PerperStream | undefined;
  cacheOptions: CacheOptions | undefined;
  ephemeral: boolean;
}

export interface StreamsWaitForListenerRequest {
  stream: PerperStream | undefined;
}

export interface StreamsWriteItemRequest {
  stream: PerperStream | undefined;
  key?: number | undefined;
  autoKey?: boolean | undefined;
  value: Any | undefined;
}

export interface StreamsListenItemsRequest {
  stream: PerperStream | undefined;
  listenerName: string;
  /** string sql_where = 11; */
  startFromLatest: boolean;
  localToData: boolean;
}

export interface StreamsListenItemsResponse {
  key: number;
  value: Any | undefined;
}

export interface StreamsMoveListenerRequest {
  stream: PerperStream | undefined;
  listenerName: string;
  reachedKey?: number | undefined;
  deleteListener?: boolean | undefined;
}

export interface StreamsDeleteRequest {
  stream: PerperStream | undefined;
}

function createBaseStreamsCreateRequest(): StreamsCreateRequest {
  return { stream: undefined, cacheOptions: undefined, ephemeral: false };
}

export const StreamsCreateRequest = {
  encode(message: StreamsCreateRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== undefined) {
      PerperStream.encode(message.stream, writer.uint32(10).fork()).ldelim();
    }
    if (message.cacheOptions !== undefined) {
      CacheOptions.encode(message.cacheOptions, writer.uint32(82).fork()).ldelim();
    }
    if (message.ephemeral === true) {
      writer.uint32(88).bool(message.ephemeral);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamsCreateRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamsCreateRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.stream = PerperStream.decode(reader, reader.uint32());
          break;
        case 10:
          message.cacheOptions = CacheOptions.decode(reader, reader.uint32());
          break;
        case 11:
          message.ephemeral = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StreamsCreateRequest {
    return {
      stream: isSet(object.stream) ? PerperStream.fromJSON(object.stream) : undefined,
      cacheOptions: isSet(object.cacheOptions) ? CacheOptions.fromJSON(object.cacheOptions) : undefined,
      ephemeral: isSet(object.ephemeral) ? Boolean(object.ephemeral) : false,
    };
  },

  toJSON(message: StreamsCreateRequest): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream ? PerperStream.toJSON(message.stream) : undefined);
    message.cacheOptions !== undefined &&
      (obj.cacheOptions = message.cacheOptions ? CacheOptions.toJSON(message.cacheOptions) : undefined);
    message.ephemeral !== undefined && (obj.ephemeral = message.ephemeral);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamsCreateRequest>, I>>(object: I): StreamsCreateRequest {
    const message = createBaseStreamsCreateRequest();
    message.stream = (object.stream !== undefined && object.stream !== null)
      ? PerperStream.fromPartial(object.stream)
      : undefined;
    message.cacheOptions = (object.cacheOptions !== undefined && object.cacheOptions !== null)
      ? CacheOptions.fromPartial(object.cacheOptions)
      : undefined;
    message.ephemeral = object.ephemeral ?? false;
    return message;
  },
};

function createBaseStreamsWaitForListenerRequest(): StreamsWaitForListenerRequest {
  return { stream: undefined };
}

export const StreamsWaitForListenerRequest = {
  encode(message: StreamsWaitForListenerRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== undefined) {
      PerperStream.encode(message.stream, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamsWaitForListenerRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamsWaitForListenerRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.stream = PerperStream.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StreamsWaitForListenerRequest {
    return { stream: isSet(object.stream) ? PerperStream.fromJSON(object.stream) : undefined };
  },

  toJSON(message: StreamsWaitForListenerRequest): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream ? PerperStream.toJSON(message.stream) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamsWaitForListenerRequest>, I>>(
    object: I,
  ): StreamsWaitForListenerRequest {
    const message = createBaseStreamsWaitForListenerRequest();
    message.stream = (object.stream !== undefined && object.stream !== null)
      ? PerperStream.fromPartial(object.stream)
      : undefined;
    return message;
  },
};

function createBaseStreamsWriteItemRequest(): StreamsWriteItemRequest {
  return { stream: undefined, key: undefined, autoKey: undefined, value: undefined };
}

export const StreamsWriteItemRequest = {
  encode(message: StreamsWriteItemRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== undefined) {
      PerperStream.encode(message.stream, writer.uint32(10).fork()).ldelim();
    }
    if (message.key !== undefined) {
      writer.uint32(16).int64(message.key);
    }
    if (message.autoKey !== undefined) {
      writer.uint32(24).bool(message.autoKey);
    }
    if (message.value !== undefined) {
      Any.encode(message.value, writer.uint32(34).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamsWriteItemRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamsWriteItemRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.stream = PerperStream.decode(reader, reader.uint32());
          break;
        case 2:
          message.key = longToNumber(reader.int64() as Long);
          break;
        case 3:
          message.autoKey = reader.bool();
          break;
        case 4:
          message.value = Any.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StreamsWriteItemRequest {
    return {
      stream: isSet(object.stream) ? PerperStream.fromJSON(object.stream) : undefined,
      key: isSet(object.key) ? Number(object.key) : undefined,
      autoKey: isSet(object.autoKey) ? Boolean(object.autoKey) : undefined,
      value: isSet(object.value) ? Any.fromJSON(object.value) : undefined,
    };
  },

  toJSON(message: StreamsWriteItemRequest): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream ? PerperStream.toJSON(message.stream) : undefined);
    message.key !== undefined && (obj.key = Math.round(message.key));
    message.autoKey !== undefined && (obj.autoKey = message.autoKey);
    message.value !== undefined && (obj.value = message.value ? Any.toJSON(message.value) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamsWriteItemRequest>, I>>(object: I): StreamsWriteItemRequest {
    const message = createBaseStreamsWriteItemRequest();
    message.stream = (object.stream !== undefined && object.stream !== null)
      ? PerperStream.fromPartial(object.stream)
      : undefined;
    message.key = object.key ?? undefined;
    message.autoKey = object.autoKey ?? undefined;
    message.value = (object.value !== undefined && object.value !== null) ? Any.fromPartial(object.value) : undefined;
    return message;
  },
};

function createBaseStreamsListenItemsRequest(): StreamsListenItemsRequest {
  return { stream: undefined, listenerName: "", startFromLatest: false, localToData: false };
}

export const StreamsListenItemsRequest = {
  encode(message: StreamsListenItemsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== undefined) {
      PerperStream.encode(message.stream, writer.uint32(10).fork()).ldelim();
    }
    if (message.listenerName !== "") {
      writer.uint32(18).string(message.listenerName);
    }
    if (message.startFromLatest === true) {
      writer.uint32(80).bool(message.startFromLatest);
    }
    if (message.localToData === true) {
      writer.uint32(808).bool(message.localToData);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamsListenItemsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamsListenItemsRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.stream = PerperStream.decode(reader, reader.uint32());
          break;
        case 2:
          message.listenerName = reader.string();
          break;
        case 10:
          message.startFromLatest = reader.bool();
          break;
        case 101:
          message.localToData = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StreamsListenItemsRequest {
    return {
      stream: isSet(object.stream) ? PerperStream.fromJSON(object.stream) : undefined,
      listenerName: isSet(object.listenerName) ? String(object.listenerName) : "",
      startFromLatest: isSet(object.startFromLatest) ? Boolean(object.startFromLatest) : false,
      localToData: isSet(object.localToData) ? Boolean(object.localToData) : false,
    };
  },

  toJSON(message: StreamsListenItemsRequest): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream ? PerperStream.toJSON(message.stream) : undefined);
    message.listenerName !== undefined && (obj.listenerName = message.listenerName);
    message.startFromLatest !== undefined && (obj.startFromLatest = message.startFromLatest);
    message.localToData !== undefined && (obj.localToData = message.localToData);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamsListenItemsRequest>, I>>(object: I): StreamsListenItemsRequest {
    const message = createBaseStreamsListenItemsRequest();
    message.stream = (object.stream !== undefined && object.stream !== null)
      ? PerperStream.fromPartial(object.stream)
      : undefined;
    message.listenerName = object.listenerName ?? "";
    message.startFromLatest = object.startFromLatest ?? false;
    message.localToData = object.localToData ?? false;
    return message;
  },
};

function createBaseStreamsListenItemsResponse(): StreamsListenItemsResponse {
  return { key: 0, value: undefined };
}

export const StreamsListenItemsResponse = {
  encode(message: StreamsListenItemsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.key !== 0) {
      writer.uint32(8).int64(message.key);
    }
    if (message.value !== undefined) {
      Any.encode(message.value, writer.uint32(18).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamsListenItemsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamsListenItemsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.key = longToNumber(reader.int64() as Long);
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

  fromJSON(object: any): StreamsListenItemsResponse {
    return {
      key: isSet(object.key) ? Number(object.key) : 0,
      value: isSet(object.value) ? Any.fromJSON(object.value) : undefined,
    };
  },

  toJSON(message: StreamsListenItemsResponse): unknown {
    const obj: any = {};
    message.key !== undefined && (obj.key = Math.round(message.key));
    message.value !== undefined && (obj.value = message.value ? Any.toJSON(message.value) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamsListenItemsResponse>, I>>(object: I): StreamsListenItemsResponse {
    const message = createBaseStreamsListenItemsResponse();
    message.key = object.key ?? 0;
    message.value = (object.value !== undefined && object.value !== null) ? Any.fromPartial(object.value) : undefined;
    return message;
  },
};

function createBaseStreamsMoveListenerRequest(): StreamsMoveListenerRequest {
  return { stream: undefined, listenerName: "", reachedKey: undefined, deleteListener: undefined };
}

export const StreamsMoveListenerRequest = {
  encode(message: StreamsMoveListenerRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== undefined) {
      PerperStream.encode(message.stream, writer.uint32(10).fork()).ldelim();
    }
    if (message.listenerName !== "") {
      writer.uint32(18).string(message.listenerName);
    }
    if (message.reachedKey !== undefined) {
      writer.uint32(24).int64(message.reachedKey);
    }
    if (message.deleteListener !== undefined) {
      writer.uint32(32).bool(message.deleteListener);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamsMoveListenerRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamsMoveListenerRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.stream = PerperStream.decode(reader, reader.uint32());
          break;
        case 2:
          message.listenerName = reader.string();
          break;
        case 3:
          message.reachedKey = longToNumber(reader.int64() as Long);
          break;
        case 4:
          message.deleteListener = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StreamsMoveListenerRequest {
    return {
      stream: isSet(object.stream) ? PerperStream.fromJSON(object.stream) : undefined,
      listenerName: isSet(object.listenerName) ? String(object.listenerName) : "",
      reachedKey: isSet(object.reachedKey) ? Number(object.reachedKey) : undefined,
      deleteListener: isSet(object.deleteListener) ? Boolean(object.deleteListener) : undefined,
    };
  },

  toJSON(message: StreamsMoveListenerRequest): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream ? PerperStream.toJSON(message.stream) : undefined);
    message.listenerName !== undefined && (obj.listenerName = message.listenerName);
    message.reachedKey !== undefined && (obj.reachedKey = Math.round(message.reachedKey));
    message.deleteListener !== undefined && (obj.deleteListener = message.deleteListener);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamsMoveListenerRequest>, I>>(object: I): StreamsMoveListenerRequest {
    const message = createBaseStreamsMoveListenerRequest();
    message.stream = (object.stream !== undefined && object.stream !== null)
      ? PerperStream.fromPartial(object.stream)
      : undefined;
    message.listenerName = object.listenerName ?? "";
    message.reachedKey = object.reachedKey ?? undefined;
    message.deleteListener = object.deleteListener ?? undefined;
    return message;
  },
};

function createBaseStreamsDeleteRequest(): StreamsDeleteRequest {
  return { stream: undefined };
}

export const StreamsDeleteRequest = {
  encode(message: StreamsDeleteRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== undefined) {
      PerperStream.encode(message.stream, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamsDeleteRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamsDeleteRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.stream = PerperStream.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StreamsDeleteRequest {
    return { stream: isSet(object.stream) ? PerperStream.fromJSON(object.stream) : undefined };
  },

  toJSON(message: StreamsDeleteRequest): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream ? PerperStream.toJSON(message.stream) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamsDeleteRequest>, I>>(object: I): StreamsDeleteRequest {
    const message = createBaseStreamsDeleteRequest();
    message.stream = (object.stream !== undefined && object.stream !== null)
      ? PerperStream.fromPartial(object.stream)
      : undefined;
    return message;
  },
};

export interface FabricStreams {
  Create(request: DeepPartial<StreamsCreateRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  WriteItem(request: DeepPartial<StreamsWriteItemRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  ListenItems(
    request: DeepPartial<StreamsListenItemsRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StreamsListenItemsResponse>;
  MoveListener(request: DeepPartial<StreamsMoveListenerRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  WaitForListener(request: DeepPartial<StreamsWaitForListenerRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  Delete(request: DeepPartial<StreamsDeleteRequest>, metadata?: grpc.Metadata): Promise<Empty>;
}

export class FabricStreamsClientImpl implements FabricStreams {
  private readonly rpc: Rpc;

  constructor(rpc: Rpc) {
    this.rpc = rpc;
    this.Create = this.Create.bind(this);
    this.WriteItem = this.WriteItem.bind(this);
    this.ListenItems = this.ListenItems.bind(this);
    this.MoveListener = this.MoveListener.bind(this);
    this.WaitForListener = this.WaitForListener.bind(this);
    this.Delete = this.Delete.bind(this);
  }

  Create(request: DeepPartial<StreamsCreateRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricStreamsCreateDesc, StreamsCreateRequest.fromPartial(request), metadata);
  }

  WriteItem(request: DeepPartial<StreamsWriteItemRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricStreamsWriteItemDesc, StreamsWriteItemRequest.fromPartial(request), metadata);
  }

  ListenItems(
    request: DeepPartial<StreamsListenItemsRequest>,
    metadata?: grpc.Metadata,
  ): Observable<StreamsListenItemsResponse> {
    return this.rpc.invoke(FabricStreamsListenItemsDesc, StreamsListenItemsRequest.fromPartial(request), metadata);
  }

  MoveListener(request: DeepPartial<StreamsMoveListenerRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricStreamsMoveListenerDesc, StreamsMoveListenerRequest.fromPartial(request), metadata);
  }

  WaitForListener(request: DeepPartial<StreamsWaitForListenerRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(
      FabricStreamsWaitForListenerDesc,
      StreamsWaitForListenerRequest.fromPartial(request),
      metadata,
    );
  }

  Delete(request: DeepPartial<StreamsDeleteRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricStreamsDeleteDesc, StreamsDeleteRequest.fromPartial(request), metadata);
  }
}

export const FabricStreamsDesc = { serviceName: "perper.grpc2.FabricStreams" };

export const FabricStreamsCreateDesc: UnaryMethodDefinitionish = {
  methodName: "Create",
  service: FabricStreamsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StreamsCreateRequest.encode(this).finish();
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

export const FabricStreamsWriteItemDesc: UnaryMethodDefinitionish = {
  methodName: "WriteItem",
  service: FabricStreamsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StreamsWriteItemRequest.encode(this).finish();
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

export const FabricStreamsListenItemsDesc: UnaryMethodDefinitionish = {
  methodName: "ListenItems",
  service: FabricStreamsDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return StreamsListenItemsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StreamsListenItemsResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricStreamsMoveListenerDesc: UnaryMethodDefinitionish = {
  methodName: "MoveListener",
  service: FabricStreamsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StreamsMoveListenerRequest.encode(this).finish();
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

export const FabricStreamsWaitForListenerDesc: UnaryMethodDefinitionish = {
  methodName: "WaitForListener",
  service: FabricStreamsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StreamsWaitForListenerRequest.encode(this).finish();
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

export const FabricStreamsDeleteDesc: UnaryMethodDefinitionish = {
  methodName: "Delete",
  service: FabricStreamsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return StreamsDeleteRequest.encode(this).finish();
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
