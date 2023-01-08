/* eslint-disable */
import { grpc } from "@improbable-eng/grpc-web";
import { BrowserHeaders } from "browser-headers";
import Long from "long";
import _m0 from "protobufjs/minimal";
import { Observable } from "rxjs";
import { share } from "rxjs/operators";

export const protobufPackage = "externalscaler";

/** Source: https://github.com/kedacore/keda/blob/a9e118f76d846a87d19d224a858009e7650f2fc5/pkg/scalers/externalscaler/externalscaler.proto */

export interface ScaledObjectRef {
  name: string;
  namespace: string;
  scalerMetadata: { [key: string]: string };
}

export interface ScaledObjectRef_ScalerMetadataEntry {
  key: string;
  value: string;
}

export interface IsActiveResponse {
  result: boolean;
}

export interface GetMetricSpecResponse {
  metricSpecs: MetricSpec[];
}

export interface MetricSpec {
  metricName: string;
  targetSize: number;
}

export interface GetMetricsRequest {
  scaledObjectRef: ScaledObjectRef | undefined;
  metricName: string;
}

export interface GetMetricsResponse {
  metricValues: MetricValue[];
}

export interface MetricValue {
  metricName: string;
  metricValue: number;
}

function createBaseScaledObjectRef(): ScaledObjectRef {
  return { name: "", namespace: "", scalerMetadata: {} };
}

export const ScaledObjectRef = {
  encode(message: ScaledObjectRef, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.name !== "") {
      writer.uint32(10).string(message.name);
    }
    if (message.namespace !== "") {
      writer.uint32(18).string(message.namespace);
    }
    Object.entries(message.scalerMetadata).forEach(([key, value]) => {
      ScaledObjectRef_ScalerMetadataEntry.encode({ key: key as any, value }, writer.uint32(26).fork()).ldelim();
    });
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ScaledObjectRef {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseScaledObjectRef();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.name = reader.string();
          break;
        case 2:
          message.namespace = reader.string();
          break;
        case 3:
          const entry3 = ScaledObjectRef_ScalerMetadataEntry.decode(reader, reader.uint32());
          if (entry3.value !== undefined) {
            message.scalerMetadata[entry3.key] = entry3.value;
          }
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ScaledObjectRef {
    return {
      name: isSet(object.name) ? String(object.name) : "",
      namespace: isSet(object.namespace) ? String(object.namespace) : "",
      scalerMetadata: isObject(object.scalerMetadata)
        ? Object.entries(object.scalerMetadata).reduce<{ [key: string]: string }>((acc, [key, value]) => {
          acc[key] = String(value);
          return acc;
        }, {})
        : {},
    };
  },

  toJSON(message: ScaledObjectRef): unknown {
    const obj: any = {};
    message.name !== undefined && (obj.name = message.name);
    message.namespace !== undefined && (obj.namespace = message.namespace);
    obj.scalerMetadata = {};
    if (message.scalerMetadata) {
      Object.entries(message.scalerMetadata).forEach(([k, v]) => {
        obj.scalerMetadata[k] = v;
      });
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ScaledObjectRef>, I>>(object: I): ScaledObjectRef {
    const message = createBaseScaledObjectRef();
    message.name = object.name ?? "";
    message.namespace = object.namespace ?? "";
    message.scalerMetadata = Object.entries(object.scalerMetadata ?? {}).reduce<{ [key: string]: string }>(
      (acc, [key, value]) => {
        if (value !== undefined) {
          acc[key] = String(value);
        }
        return acc;
      },
      {},
    );
    return message;
  },
};

function createBaseScaledObjectRef_ScalerMetadataEntry(): ScaledObjectRef_ScalerMetadataEntry {
  return { key: "", value: "" };
}

export const ScaledObjectRef_ScalerMetadataEntry = {
  encode(message: ScaledObjectRef_ScalerMetadataEntry, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.key !== "") {
      writer.uint32(10).string(message.key);
    }
    if (message.value !== "") {
      writer.uint32(18).string(message.value);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ScaledObjectRef_ScalerMetadataEntry {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseScaledObjectRef_ScalerMetadataEntry();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.key = reader.string();
          break;
        case 2:
          message.value = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ScaledObjectRef_ScalerMetadataEntry {
    return { key: isSet(object.key) ? String(object.key) : "", value: isSet(object.value) ? String(object.value) : "" };
  },

  toJSON(message: ScaledObjectRef_ScalerMetadataEntry): unknown {
    const obj: any = {};
    message.key !== undefined && (obj.key = message.key);
    message.value !== undefined && (obj.value = message.value);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ScaledObjectRef_ScalerMetadataEntry>, I>>(
    object: I,
  ): ScaledObjectRef_ScalerMetadataEntry {
    const message = createBaseScaledObjectRef_ScalerMetadataEntry();
    message.key = object.key ?? "";
    message.value = object.value ?? "";
    return message;
  },
};

function createBaseIsActiveResponse(): IsActiveResponse {
  return { result: false };
}

export const IsActiveResponse = {
  encode(message: IsActiveResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.result === true) {
      writer.uint32(8).bool(message.result);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): IsActiveResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseIsActiveResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.result = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): IsActiveResponse {
    return { result: isSet(object.result) ? Boolean(object.result) : false };
  },

  toJSON(message: IsActiveResponse): unknown {
    const obj: any = {};
    message.result !== undefined && (obj.result = message.result);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<IsActiveResponse>, I>>(object: I): IsActiveResponse {
    const message = createBaseIsActiveResponse();
    message.result = object.result ?? false;
    return message;
  },
};

function createBaseGetMetricSpecResponse(): GetMetricSpecResponse {
  return { metricSpecs: [] };
}

export const GetMetricSpecResponse = {
  encode(message: GetMetricSpecResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    for (const v of message.metricSpecs) {
      MetricSpec.encode(v!, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): GetMetricSpecResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseGetMetricSpecResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.metricSpecs.push(MetricSpec.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): GetMetricSpecResponse {
    return {
      metricSpecs: Array.isArray(object?.metricSpecs) ? object.metricSpecs.map((e: any) => MetricSpec.fromJSON(e)) : [],
    };
  },

  toJSON(message: GetMetricSpecResponse): unknown {
    const obj: any = {};
    if (message.metricSpecs) {
      obj.metricSpecs = message.metricSpecs.map((e) => e ? MetricSpec.toJSON(e) : undefined);
    } else {
      obj.metricSpecs = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<GetMetricSpecResponse>, I>>(object: I): GetMetricSpecResponse {
    const message = createBaseGetMetricSpecResponse();
    message.metricSpecs = object.metricSpecs?.map((e) => MetricSpec.fromPartial(e)) || [];
    return message;
  },
};

function createBaseMetricSpec(): MetricSpec {
  return { metricName: "", targetSize: 0 };
}

export const MetricSpec = {
  encode(message: MetricSpec, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.metricName !== "") {
      writer.uint32(10).string(message.metricName);
    }
    if (message.targetSize !== 0) {
      writer.uint32(16).int64(message.targetSize);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): MetricSpec {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseMetricSpec();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.metricName = reader.string();
          break;
        case 2:
          message.targetSize = longToNumber(reader.int64() as Long);
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): MetricSpec {
    return {
      metricName: isSet(object.metricName) ? String(object.metricName) : "",
      targetSize: isSet(object.targetSize) ? Number(object.targetSize) : 0,
    };
  },

  toJSON(message: MetricSpec): unknown {
    const obj: any = {};
    message.metricName !== undefined && (obj.metricName = message.metricName);
    message.targetSize !== undefined && (obj.targetSize = Math.round(message.targetSize));
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<MetricSpec>, I>>(object: I): MetricSpec {
    const message = createBaseMetricSpec();
    message.metricName = object.metricName ?? "";
    message.targetSize = object.targetSize ?? 0;
    return message;
  },
};

function createBaseGetMetricsRequest(): GetMetricsRequest {
  return { scaledObjectRef: undefined, metricName: "" };
}

export const GetMetricsRequest = {
  encode(message: GetMetricsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.scaledObjectRef !== undefined) {
      ScaledObjectRef.encode(message.scaledObjectRef, writer.uint32(10).fork()).ldelim();
    }
    if (message.metricName !== "") {
      writer.uint32(18).string(message.metricName);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): GetMetricsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseGetMetricsRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.scaledObjectRef = ScaledObjectRef.decode(reader, reader.uint32());
          break;
        case 2:
          message.metricName = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): GetMetricsRequest {
    return {
      scaledObjectRef: isSet(object.scaledObjectRef) ? ScaledObjectRef.fromJSON(object.scaledObjectRef) : undefined,
      metricName: isSet(object.metricName) ? String(object.metricName) : "",
    };
  },

  toJSON(message: GetMetricsRequest): unknown {
    const obj: any = {};
    message.scaledObjectRef !== undefined &&
      (obj.scaledObjectRef = message.scaledObjectRef ? ScaledObjectRef.toJSON(message.scaledObjectRef) : undefined);
    message.metricName !== undefined && (obj.metricName = message.metricName);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<GetMetricsRequest>, I>>(object: I): GetMetricsRequest {
    const message = createBaseGetMetricsRequest();
    message.scaledObjectRef = (object.scaledObjectRef !== undefined && object.scaledObjectRef !== null)
      ? ScaledObjectRef.fromPartial(object.scaledObjectRef)
      : undefined;
    message.metricName = object.metricName ?? "";
    return message;
  },
};

function createBaseGetMetricsResponse(): GetMetricsResponse {
  return { metricValues: [] };
}

export const GetMetricsResponse = {
  encode(message: GetMetricsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    for (const v of message.metricValues) {
      MetricValue.encode(v!, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): GetMetricsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseGetMetricsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.metricValues.push(MetricValue.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): GetMetricsResponse {
    return {
      metricValues: Array.isArray(object?.metricValues)
        ? object.metricValues.map((e: any) => MetricValue.fromJSON(e))
        : [],
    };
  },

  toJSON(message: GetMetricsResponse): unknown {
    const obj: any = {};
    if (message.metricValues) {
      obj.metricValues = message.metricValues.map((e) => e ? MetricValue.toJSON(e) : undefined);
    } else {
      obj.metricValues = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<GetMetricsResponse>, I>>(object: I): GetMetricsResponse {
    const message = createBaseGetMetricsResponse();
    message.metricValues = object.metricValues?.map((e) => MetricValue.fromPartial(e)) || [];
    return message;
  },
};

function createBaseMetricValue(): MetricValue {
  return { metricName: "", metricValue: 0 };
}

export const MetricValue = {
  encode(message: MetricValue, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.metricName !== "") {
      writer.uint32(10).string(message.metricName);
    }
    if (message.metricValue !== 0) {
      writer.uint32(16).int64(message.metricValue);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): MetricValue {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseMetricValue();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.metricName = reader.string();
          break;
        case 2:
          message.metricValue = longToNumber(reader.int64() as Long);
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): MetricValue {
    return {
      metricName: isSet(object.metricName) ? String(object.metricName) : "",
      metricValue: isSet(object.metricValue) ? Number(object.metricValue) : 0,
    };
  },

  toJSON(message: MetricValue): unknown {
    const obj: any = {};
    message.metricName !== undefined && (obj.metricName = message.metricName);
    message.metricValue !== undefined && (obj.metricValue = Math.round(message.metricValue));
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<MetricValue>, I>>(object: I): MetricValue {
    const message = createBaseMetricValue();
    message.metricName = object.metricName ?? "";
    message.metricValue = object.metricValue ?? 0;
    return message;
  },
};

export interface ExternalScaler {
  IsActive(request: DeepPartial<ScaledObjectRef>, metadata?: grpc.Metadata): Promise<IsActiveResponse>;
  StreamIsActive(request: DeepPartial<ScaledObjectRef>, metadata?: grpc.Metadata): Observable<IsActiveResponse>;
  GetMetricSpec(request: DeepPartial<ScaledObjectRef>, metadata?: grpc.Metadata): Promise<GetMetricSpecResponse>;
  GetMetrics(request: DeepPartial<GetMetricsRequest>, metadata?: grpc.Metadata): Promise<GetMetricsResponse>;
}

export class ExternalScalerClientImpl implements ExternalScaler {
  private readonly rpc: Rpc;

  constructor(rpc: Rpc) {
    this.rpc = rpc;
    this.IsActive = this.IsActive.bind(this);
    this.StreamIsActive = this.StreamIsActive.bind(this);
    this.GetMetricSpec = this.GetMetricSpec.bind(this);
    this.GetMetrics = this.GetMetrics.bind(this);
  }

  IsActive(request: DeepPartial<ScaledObjectRef>, metadata?: grpc.Metadata): Promise<IsActiveResponse> {
    return this.rpc.unary(ExternalScalerIsActiveDesc, ScaledObjectRef.fromPartial(request), metadata);
  }

  StreamIsActive(request: DeepPartial<ScaledObjectRef>, metadata?: grpc.Metadata): Observable<IsActiveResponse> {
    return this.rpc.invoke(ExternalScalerStreamIsActiveDesc, ScaledObjectRef.fromPartial(request), metadata);
  }

  GetMetricSpec(request: DeepPartial<ScaledObjectRef>, metadata?: grpc.Metadata): Promise<GetMetricSpecResponse> {
    return this.rpc.unary(ExternalScalerGetMetricSpecDesc, ScaledObjectRef.fromPartial(request), metadata);
  }

  GetMetrics(request: DeepPartial<GetMetricsRequest>, metadata?: grpc.Metadata): Promise<GetMetricsResponse> {
    return this.rpc.unary(ExternalScalerGetMetricsDesc, GetMetricsRequest.fromPartial(request), metadata);
  }
}

export const ExternalScalerDesc = { serviceName: "externalscaler.ExternalScaler" };

export const ExternalScalerIsActiveDesc: UnaryMethodDefinitionish = {
  methodName: "IsActive",
  service: ExternalScalerDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ScaledObjectRef.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = IsActiveResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const ExternalScalerStreamIsActiveDesc: UnaryMethodDefinitionish = {
  methodName: "StreamIsActive",
  service: ExternalScalerDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return ScaledObjectRef.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = IsActiveResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const ExternalScalerGetMetricSpecDesc: UnaryMethodDefinitionish = {
  methodName: "GetMetricSpec",
  service: ExternalScalerDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ScaledObjectRef.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = GetMetricSpecResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const ExternalScalerGetMetricsDesc: UnaryMethodDefinitionish = {
  methodName: "GetMetrics",
  service: ExternalScalerDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return GetMetricsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = GetMetricsResponse.decode(data);
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

function isObject(value: any): boolean {
  return typeof value === "object" && value !== null;
}

function isSet(value: any): boolean {
  return value !== null && value !== undefined;
}

export class GrpcWebError extends tsProtoGlobalThis.Error {
  constructor(message: string, public code: grpc.Code, public metadata: grpc.Metadata) {
    super(message);
  }
}
