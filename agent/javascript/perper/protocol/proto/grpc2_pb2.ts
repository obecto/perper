/* eslint-disable */
import { grpc } from "@improbable-eng/grpc-web";
import { BrowserHeaders } from "browser-headers";
import _m0 from "protobufjs/minimal";
import { Empty } from "./google/protobuf/empty_pb2";
import { Enum, Type } from "./google/protobuf/type_pb2";

export const protobufPackage = "perper.grpc2";

export interface FabricProtobufDescriptorsRegisterRequest {
  typeUrl: string;
  type?: Type | undefined;
  enum?: Enum | undefined;
}

export interface FabricProtobufDescriptorsGetRequest {
  typeUrl: string;
}

export interface FabricProtobufDescriptorsGetResponse {
  type?: Type | undefined;
  enum?: Enum | undefined;
}

function createBaseFabricProtobufDescriptorsRegisterRequest(): FabricProtobufDescriptorsRegisterRequest {
  return { typeUrl: "", type: undefined, enum: undefined };
}

export const FabricProtobufDescriptorsRegisterRequest = {
  encode(message: FabricProtobufDescriptorsRegisterRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.typeUrl !== "") {
      writer.uint32(10).string(message.typeUrl);
    }
    if (message.type !== undefined) {
      Type.encode(message.type, writer.uint32(18).fork()).ldelim();
    }
    if (message.enum !== undefined) {
      Enum.encode(message.enum, writer.uint32(26).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): FabricProtobufDescriptorsRegisterRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseFabricProtobufDescriptorsRegisterRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.typeUrl = reader.string();
          break;
        case 2:
          message.type = Type.decode(reader, reader.uint32());
          break;
        case 3:
          message.enum = Enum.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): FabricProtobufDescriptorsRegisterRequest {
    return {
      typeUrl: isSet(object.typeUrl) ? String(object.typeUrl) : "",
      type: isSet(object.type) ? Type.fromJSON(object.type) : undefined,
      enum: isSet(object.enum) ? Enum.fromJSON(object.enum) : undefined,
    };
  },

  toJSON(message: FabricProtobufDescriptorsRegisterRequest): unknown {
    const obj: any = {};
    message.typeUrl !== undefined && (obj.typeUrl = message.typeUrl);
    message.type !== undefined && (obj.type = message.type ? Type.toJSON(message.type) : undefined);
    message.enum !== undefined && (obj.enum = message.enum ? Enum.toJSON(message.enum) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<FabricProtobufDescriptorsRegisterRequest>, I>>(
    object: I,
  ): FabricProtobufDescriptorsRegisterRequest {
    const message = createBaseFabricProtobufDescriptorsRegisterRequest();
    message.typeUrl = object.typeUrl ?? "";
    message.type = (object.type !== undefined && object.type !== null) ? Type.fromPartial(object.type) : undefined;
    message.enum = (object.enum !== undefined && object.enum !== null) ? Enum.fromPartial(object.enum) : undefined;
    return message;
  },
};

function createBaseFabricProtobufDescriptorsGetRequest(): FabricProtobufDescriptorsGetRequest {
  return { typeUrl: "" };
}

export const FabricProtobufDescriptorsGetRequest = {
  encode(message: FabricProtobufDescriptorsGetRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.typeUrl !== "") {
      writer.uint32(10).string(message.typeUrl);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): FabricProtobufDescriptorsGetRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseFabricProtobufDescriptorsGetRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.typeUrl = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): FabricProtobufDescriptorsGetRequest {
    return { typeUrl: isSet(object.typeUrl) ? String(object.typeUrl) : "" };
  },

  toJSON(message: FabricProtobufDescriptorsGetRequest): unknown {
    const obj: any = {};
    message.typeUrl !== undefined && (obj.typeUrl = message.typeUrl);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<FabricProtobufDescriptorsGetRequest>, I>>(
    object: I,
  ): FabricProtobufDescriptorsGetRequest {
    const message = createBaseFabricProtobufDescriptorsGetRequest();
    message.typeUrl = object.typeUrl ?? "";
    return message;
  },
};

function createBaseFabricProtobufDescriptorsGetResponse(): FabricProtobufDescriptorsGetResponse {
  return { type: undefined, enum: undefined };
}

export const FabricProtobufDescriptorsGetResponse = {
  encode(message: FabricProtobufDescriptorsGetResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.type !== undefined) {
      Type.encode(message.type, writer.uint32(18).fork()).ldelim();
    }
    if (message.enum !== undefined) {
      Enum.encode(message.enum, writer.uint32(26).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): FabricProtobufDescriptorsGetResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseFabricProtobufDescriptorsGetResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 2:
          message.type = Type.decode(reader, reader.uint32());
          break;
        case 3:
          message.enum = Enum.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): FabricProtobufDescriptorsGetResponse {
    return {
      type: isSet(object.type) ? Type.fromJSON(object.type) : undefined,
      enum: isSet(object.enum) ? Enum.fromJSON(object.enum) : undefined,
    };
  },

  toJSON(message: FabricProtobufDescriptorsGetResponse): unknown {
    const obj: any = {};
    message.type !== undefined && (obj.type = message.type ? Type.toJSON(message.type) : undefined);
    message.enum !== undefined && (obj.enum = message.enum ? Enum.toJSON(message.enum) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<FabricProtobufDescriptorsGetResponse>, I>>(
    object: I,
  ): FabricProtobufDescriptorsGetResponse {
    const message = createBaseFabricProtobufDescriptorsGetResponse();
    message.type = (object.type !== undefined && object.type !== null) ? Type.fromPartial(object.type) : undefined;
    message.enum = (object.enum !== undefined && object.enum !== null) ? Enum.fromPartial(object.enum) : undefined;
    return message;
  },
};

export interface FabricProtobufDescriptors {
  Register(request: DeepPartial<FabricProtobufDescriptorsRegisterRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  Get(
    request: DeepPartial<FabricProtobufDescriptorsGetRequest>,
    metadata?: grpc.Metadata,
  ): Promise<FabricProtobufDescriptorsGetResponse>;
}

export class FabricProtobufDescriptorsClientImpl implements FabricProtobufDescriptors {
  private readonly rpc: Rpc;

  constructor(rpc: Rpc) {
    this.rpc = rpc;
    this.Register = this.Register.bind(this);
    this.Get = this.Get.bind(this);
  }

  Register(request: DeepPartial<FabricProtobufDescriptorsRegisterRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(
      FabricProtobufDescriptorsRegisterDesc,
      FabricProtobufDescriptorsRegisterRequest.fromPartial(request),
      metadata,
    );
  }

  Get(
    request: DeepPartial<FabricProtobufDescriptorsGetRequest>,
    metadata?: grpc.Metadata,
  ): Promise<FabricProtobufDescriptorsGetResponse> {
    return this.rpc.unary(
      FabricProtobufDescriptorsGetDesc,
      FabricProtobufDescriptorsGetRequest.fromPartial(request),
      metadata,
    );
  }
}

export const FabricProtobufDescriptorsDesc = { serviceName: "perper.grpc2.FabricProtobufDescriptors" };

export const FabricProtobufDescriptorsRegisterDesc: UnaryMethodDefinitionish = {
  methodName: "Register",
  service: FabricProtobufDescriptorsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return FabricProtobufDescriptorsRegisterRequest.encode(this).finish();
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

export const FabricProtobufDescriptorsGetDesc: UnaryMethodDefinitionish = {
  methodName: "Get",
  service: FabricProtobufDescriptorsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return FabricProtobufDescriptorsGetRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = FabricProtobufDescriptorsGetResponse.decode(data);
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
}

export class GrpcWebImpl {
  private host: string;
  private options: {
    transport?: grpc.TransportFactory;

    debug?: boolean;
    metadata?: grpc.Metadata;
    upStreamRetryCodes?: number[];
  };

  constructor(
    host: string,
    options: {
      transport?: grpc.TransportFactory;

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

function isSet(value: any): boolean {
  return value !== null && value !== undefined;
}

export class GrpcWebError extends tsProtoGlobalThis.Error {
  constructor(message: string, public code: grpc.Code, public metadata: grpc.Metadata) {
    super(message);
  }
}
