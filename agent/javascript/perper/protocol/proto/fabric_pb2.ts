/* eslint-disable */
import { grpc } from "@improbable-eng/grpc-web";
import { BrowserHeaders } from "browser-headers";
import Long from "long";
import _m0 from "protobufjs/minimal";
import { Observable } from "rxjs";
import { share } from "rxjs/operators";
import { Empty } from "./google/protobuf/empty_pb2";

export const protobufPackage = "perper";

export interface AllExecutionsResponse {
  executions: ExecutionsResponse[];
}

export interface ExecutionsRequest {
  agent: string;
  instance: string;
  delegate: string;
  localToData: boolean;
}

export interface ExecutionsResponse {
  /**
   * string agent = 1;
   * Can possibly remove instance and delegate (99% the agent will be reading the execution data for parameters anyway)
   */
  instance: string;
  delegate: string;
  execution: string;
  cancelled: boolean;
  startOfStream: boolean;
}

export interface ReservedExecutionsRequest {
  reserveNext: number;
  /** Only send with first message: */
  workGroup: string;
  filter: ExecutionsRequest | undefined;
}

export interface ReserveExecutionRequest {
  execution: string;
  workGroup: string;
}

export interface ExecutionFinishedRequest {
  execution: string;
}

export interface StreamItemsRequest {
  stream: string;
  startKey: number;
  localToData: boolean;
  /** 0 if realtime, 1.. to wait for stream items */
  stride: number;
}

export interface StreamItemsResponse {
  key: number;
}

export interface ListenerAttachedRequest {
  stream: string;
}

function createBaseAllExecutionsResponse(): AllExecutionsResponse {
  return { executions: [] };
}

export const AllExecutionsResponse = {
  encode(message: AllExecutionsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    for (const v of message.executions) {
      ExecutionsResponse.encode(v!, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): AllExecutionsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseAllExecutionsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.executions.push(ExecutionsResponse.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): AllExecutionsResponse {
    return {
      executions: Array.isArray(object?.executions)
        ? object.executions.map((e: any) => ExecutionsResponse.fromJSON(e))
        : [],
    };
  },

  toJSON(message: AllExecutionsResponse): unknown {
    const obj: any = {};
    if (message.executions) {
      obj.executions = message.executions.map((e) => e ? ExecutionsResponse.toJSON(e) : undefined);
    } else {
      obj.executions = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<AllExecutionsResponse>, I>>(object: I): AllExecutionsResponse {
    const message = createBaseAllExecutionsResponse();
    message.executions = object.executions?.map((e) => ExecutionsResponse.fromPartial(e)) || [];
    return message;
  },
};

function createBaseExecutionsRequest(): ExecutionsRequest {
  return { agent: "", instance: "", delegate: "", localToData: false };
}

export const ExecutionsRequest = {
  encode(message: ExecutionsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.agent !== "") {
      writer.uint32(10).string(message.agent);
    }
    if (message.instance !== "") {
      writer.uint32(18).string(message.instance);
    }
    if (message.delegate !== "") {
      writer.uint32(26).string(message.delegate);
    }
    if (message.localToData === true) {
      writer.uint32(32).bool(message.localToData);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.agent = reader.string();
          break;
        case 2:
          message.instance = reader.string();
          break;
        case 3:
          message.delegate = reader.string();
          break;
        case 4:
          message.localToData = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsRequest {
    return {
      agent: isSet(object.agent) ? String(object.agent) : "",
      instance: isSet(object.instance) ? String(object.instance) : "",
      delegate: isSet(object.delegate) ? String(object.delegate) : "",
      localToData: isSet(object.localToData) ? Boolean(object.localToData) : false,
    };
  },

  toJSON(message: ExecutionsRequest): unknown {
    const obj: any = {};
    message.agent !== undefined && (obj.agent = message.agent);
    message.instance !== undefined && (obj.instance = message.instance);
    message.delegate !== undefined && (obj.delegate = message.delegate);
    message.localToData !== undefined && (obj.localToData = message.localToData);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsRequest>, I>>(object: I): ExecutionsRequest {
    const message = createBaseExecutionsRequest();
    message.agent = object.agent ?? "";
    message.instance = object.instance ?? "";
    message.delegate = object.delegate ?? "";
    message.localToData = object.localToData ?? false;
    return message;
  },
};

function createBaseExecutionsResponse(): ExecutionsResponse {
  return { instance: "", delegate: "", execution: "", cancelled: false, startOfStream: false };
}

export const ExecutionsResponse = {
  encode(message: ExecutionsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.instance !== "") {
      writer.uint32(18).string(message.instance);
    }
    if (message.delegate !== "") {
      writer.uint32(26).string(message.delegate);
    }
    if (message.execution !== "") {
      writer.uint32(34).string(message.execution);
    }
    if (message.cancelled === true) {
      writer.uint32(40).bool(message.cancelled);
    }
    if (message.startOfStream === true) {
      writer.uint32(80).bool(message.startOfStream);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 2:
          message.instance = reader.string();
          break;
        case 3:
          message.delegate = reader.string();
          break;
        case 4:
          message.execution = reader.string();
          break;
        case 5:
          message.cancelled = reader.bool();
          break;
        case 10:
          message.startOfStream = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsResponse {
    return {
      instance: isSet(object.instance) ? String(object.instance) : "",
      delegate: isSet(object.delegate) ? String(object.delegate) : "",
      execution: isSet(object.execution) ? String(object.execution) : "",
      cancelled: isSet(object.cancelled) ? Boolean(object.cancelled) : false,
      startOfStream: isSet(object.startOfStream) ? Boolean(object.startOfStream) : false,
    };
  },

  toJSON(message: ExecutionsResponse): unknown {
    const obj: any = {};
    message.instance !== undefined && (obj.instance = message.instance);
    message.delegate !== undefined && (obj.delegate = message.delegate);
    message.execution !== undefined && (obj.execution = message.execution);
    message.cancelled !== undefined && (obj.cancelled = message.cancelled);
    message.startOfStream !== undefined && (obj.startOfStream = message.startOfStream);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsResponse>, I>>(object: I): ExecutionsResponse {
    const message = createBaseExecutionsResponse();
    message.instance = object.instance ?? "";
    message.delegate = object.delegate ?? "";
    message.execution = object.execution ?? "";
    message.cancelled = object.cancelled ?? false;
    message.startOfStream = object.startOfStream ?? false;
    return message;
  },
};

function createBaseReservedExecutionsRequest(): ReservedExecutionsRequest {
  return { reserveNext: 0, workGroup: "", filter: undefined };
}

export const ReservedExecutionsRequest = {
  encode(message: ReservedExecutionsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.reserveNext !== 0) {
      writer.uint32(8).uint64(message.reserveNext);
    }
    if (message.workGroup !== "") {
      writer.uint32(42).string(message.workGroup);
    }
    if (message.filter !== undefined) {
      ExecutionsRequest.encode(message.filter, writer.uint32(82).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ReservedExecutionsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseReservedExecutionsRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.reserveNext = longToNumber(reader.uint64() as Long);
          break;
        case 5:
          message.workGroup = reader.string();
          break;
        case 10:
          message.filter = ExecutionsRequest.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ReservedExecutionsRequest {
    return {
      reserveNext: isSet(object.reserveNext) ? Number(object.reserveNext) : 0,
      workGroup: isSet(object.workGroup) ? String(object.workGroup) : "",
      filter: isSet(object.filter) ? ExecutionsRequest.fromJSON(object.filter) : undefined,
    };
  },

  toJSON(message: ReservedExecutionsRequest): unknown {
    const obj: any = {};
    message.reserveNext !== undefined && (obj.reserveNext = Math.round(message.reserveNext));
    message.workGroup !== undefined && (obj.workGroup = message.workGroup);
    message.filter !== undefined &&
      (obj.filter = message.filter ? ExecutionsRequest.toJSON(message.filter) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ReservedExecutionsRequest>, I>>(object: I): ReservedExecutionsRequest {
    const message = createBaseReservedExecutionsRequest();
    message.reserveNext = object.reserveNext ?? 0;
    message.workGroup = object.workGroup ?? "";
    message.filter = (object.filter !== undefined && object.filter !== null)
      ? ExecutionsRequest.fromPartial(object.filter)
      : undefined;
    return message;
  },
};

function createBaseReserveExecutionRequest(): ReserveExecutionRequest {
  return { execution: "", workGroup: "" };
}

export const ReserveExecutionRequest = {
  encode(message: ReserveExecutionRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== "") {
      writer.uint32(34).string(message.execution);
    }
    if (message.workGroup !== "") {
      writer.uint32(42).string(message.workGroup);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ReserveExecutionRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseReserveExecutionRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 4:
          message.execution = reader.string();
          break;
        case 5:
          message.workGroup = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ReserveExecutionRequest {
    return {
      execution: isSet(object.execution) ? String(object.execution) : "",
      workGroup: isSet(object.workGroup) ? String(object.workGroup) : "",
    };
  },

  toJSON(message: ReserveExecutionRequest): unknown {
    const obj: any = {};
    message.execution !== undefined && (obj.execution = message.execution);
    message.workGroup !== undefined && (obj.workGroup = message.workGroup);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ReserveExecutionRequest>, I>>(object: I): ReserveExecutionRequest {
    const message = createBaseReserveExecutionRequest();
    message.execution = object.execution ?? "";
    message.workGroup = object.workGroup ?? "";
    return message;
  },
};

function createBaseExecutionFinishedRequest(): ExecutionFinishedRequest {
  return { execution: "" };
}

export const ExecutionFinishedRequest = {
  encode(message: ExecutionFinishedRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== "") {
      writer.uint32(34).string(message.execution);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionFinishedRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionFinishedRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 4:
          message.execution = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionFinishedRequest {
    return { execution: isSet(object.execution) ? String(object.execution) : "" };
  },

  toJSON(message: ExecutionFinishedRequest): unknown {
    const obj: any = {};
    message.execution !== undefined && (obj.execution = message.execution);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionFinishedRequest>, I>>(object: I): ExecutionFinishedRequest {
    const message = createBaseExecutionFinishedRequest();
    message.execution = object.execution ?? "";
    return message;
  },
};

function createBaseStreamItemsRequest(): StreamItemsRequest {
  return { stream: "", startKey: 0, localToData: false, stride: 0 };
}

export const StreamItemsRequest = {
  encode(message: StreamItemsRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== "") {
      writer.uint32(10).string(message.stream);
    }
    if (message.startKey !== 0) {
      writer.uint32(16).int64(message.startKey);
    }
    if (message.localToData === true) {
      writer.uint32(24).bool(message.localToData);
    }
    if (message.stride !== 0) {
      writer.uint32(32).int64(message.stride);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamItemsRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamItemsRequest();
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
          message.localToData = reader.bool();
          break;
        case 4:
          message.stride = longToNumber(reader.int64() as Long);
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StreamItemsRequest {
    return {
      stream: isSet(object.stream) ? String(object.stream) : "",
      startKey: isSet(object.startKey) ? Number(object.startKey) : 0,
      localToData: isSet(object.localToData) ? Boolean(object.localToData) : false,
      stride: isSet(object.stride) ? Number(object.stride) : 0,
    };
  },

  toJSON(message: StreamItemsRequest): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream);
    message.startKey !== undefined && (obj.startKey = Math.round(message.startKey));
    message.localToData !== undefined && (obj.localToData = message.localToData);
    message.stride !== undefined && (obj.stride = Math.round(message.stride));
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamItemsRequest>, I>>(object: I): StreamItemsRequest {
    const message = createBaseStreamItemsRequest();
    message.stream = object.stream ?? "";
    message.startKey = object.startKey ?? 0;
    message.localToData = object.localToData ?? false;
    message.stride = object.stride ?? 0;
    return message;
  },
};

function createBaseStreamItemsResponse(): StreamItemsResponse {
  return { key: 0 };
}

export const StreamItemsResponse = {
  encode(message: StreamItemsResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.key !== 0) {
      writer.uint32(8).int64(message.key);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): StreamItemsResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseStreamItemsResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.key = longToNumber(reader.int64() as Long);
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): StreamItemsResponse {
    return { key: isSet(object.key) ? Number(object.key) : 0 };
  },

  toJSON(message: StreamItemsResponse): unknown {
    const obj: any = {};
    message.key !== undefined && (obj.key = Math.round(message.key));
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<StreamItemsResponse>, I>>(object: I): StreamItemsResponse {
    const message = createBaseStreamItemsResponse();
    message.key = object.key ?? 0;
    return message;
  },
};

function createBaseListenerAttachedRequest(): ListenerAttachedRequest {
  return { stream: "" };
}

export const ListenerAttachedRequest = {
  encode(message: ListenerAttachedRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.stream !== "") {
      writer.uint32(10).string(message.stream);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ListenerAttachedRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseListenerAttachedRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.stream = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ListenerAttachedRequest {
    return { stream: isSet(object.stream) ? String(object.stream) : "" };
  },

  toJSON(message: ListenerAttachedRequest): unknown {
    const obj: any = {};
    message.stream !== undefined && (obj.stream = message.stream);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ListenerAttachedRequest>, I>>(object: I): ListenerAttachedRequest {
    const message = createBaseListenerAttachedRequest();
    message.stream = object.stream ?? "";
    return message;
  },
};

export interface Fabric {
  Executions(request: DeepPartial<ExecutionsRequest>, metadata?: grpc.Metadata): Observable<ExecutionsResponse>;
  AllExecutions(request: DeepPartial<ExecutionsRequest>, metadata?: grpc.Metadata): Promise<AllExecutionsResponse>;
  ExecutionFinished(request: DeepPartial<ExecutionFinishedRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  ReserveExecution(request: DeepPartial<ReserveExecutionRequest>, metadata?: grpc.Metadata): Observable<Empty>;
  ReservedExecutions(
    request: Observable<DeepPartial<ReservedExecutionsRequest>>,
    metadata?: grpc.Metadata,
  ): Observable<ExecutionsResponse>;
  StreamItems(request: DeepPartial<StreamItemsRequest>, metadata?: grpc.Metadata): Observable<StreamItemsResponse>;
  ListenerAttached(request: DeepPartial<ListenerAttachedRequest>, metadata?: grpc.Metadata): Promise<Empty>;
}

export class FabricClientImpl implements Fabric {
  private readonly rpc: Rpc;

  constructor(rpc: Rpc) {
    this.rpc = rpc;
    this.Executions = this.Executions.bind(this);
    this.AllExecutions = this.AllExecutions.bind(this);
    this.ExecutionFinished = this.ExecutionFinished.bind(this);
    this.ReserveExecution = this.ReserveExecution.bind(this);
    this.ReservedExecutions = this.ReservedExecutions.bind(this);
    this.StreamItems = this.StreamItems.bind(this);
    this.ListenerAttached = this.ListenerAttached.bind(this);
  }

  Executions(request: DeepPartial<ExecutionsRequest>, metadata?: grpc.Metadata): Observable<ExecutionsResponse> {
    return this.rpc.invoke(FabricExecutionsDesc, ExecutionsRequest.fromPartial(request), metadata);
  }

  AllExecutions(request: DeepPartial<ExecutionsRequest>, metadata?: grpc.Metadata): Promise<AllExecutionsResponse> {
    return this.rpc.unary(FabricAllExecutionsDesc, ExecutionsRequest.fromPartial(request), metadata);
  }

  ExecutionFinished(request: DeepPartial<ExecutionFinishedRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricExecutionFinishedDesc, ExecutionFinishedRequest.fromPartial(request), metadata);
  }

  ReserveExecution(request: DeepPartial<ReserveExecutionRequest>, metadata?: grpc.Metadata): Observable<Empty> {
    return this.rpc.invoke(FabricReserveExecutionDesc, ReserveExecutionRequest.fromPartial(request), metadata);
  }

  ReservedExecutions(
    request: Observable<DeepPartial<ReservedExecutionsRequest>>,
    metadata?: grpc.Metadata,
  ): Observable<ExecutionsResponse> {
    throw new Error("ts-proto does not yet support client streaming!");
  }

  StreamItems(request: DeepPartial<StreamItemsRequest>, metadata?: grpc.Metadata): Observable<StreamItemsResponse> {
    return this.rpc.invoke(FabricStreamItemsDesc, StreamItemsRequest.fromPartial(request), metadata);
  }

  ListenerAttached(request: DeepPartial<ListenerAttachedRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricListenerAttachedDesc, ListenerAttachedRequest.fromPartial(request), metadata);
  }
}

export const FabricDesc = { serviceName: "perper.Fabric" };

export const FabricExecutionsDesc: UnaryMethodDefinitionish = {
  methodName: "Executions",
  service: FabricDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return ExecutionsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = ExecutionsResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricAllExecutionsDesc: UnaryMethodDefinitionish = {
  methodName: "AllExecutions",
  service: FabricDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ExecutionsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = AllExecutionsResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricExecutionFinishedDesc: UnaryMethodDefinitionish = {
  methodName: "ExecutionFinished",
  service: FabricDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ExecutionFinishedRequest.encode(this).finish();
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

export const FabricReserveExecutionDesc: UnaryMethodDefinitionish = {
  methodName: "ReserveExecution",
  service: FabricDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return ReserveExecutionRequest.encode(this).finish();
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

export const FabricStreamItemsDesc: UnaryMethodDefinitionish = {
  methodName: "StreamItems",
  service: FabricDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return StreamItemsRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = StreamItemsResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricListenerAttachedDesc: UnaryMethodDefinitionish = {
  methodName: "ListenerAttached",
  service: FabricDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ListenerAttachedRequest.encode(this).finish();
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
