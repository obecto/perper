/* eslint-disable */
import { grpc } from "@improbable-eng/grpc-web";
import { BrowserHeaders } from "browser-headers";
import Long from "long";
import _m0 from "protobufjs/minimal";
import { Observable } from "rxjs";
import { share } from "rxjs/operators";
import { Any } from "./google/protobuf/any_pb2";
import { Empty } from "./google/protobuf/empty_pb2";
import { PerperError, PerperExecution, PerperInstance } from "./grpc2_model_pb2";

export const protobufPackage = "perper.grpc2";

export enum LifecycleMethod {
  LifecycleMethodNull = 0,
  /** LifecycleMethodDeploy - Will be called by the runtime when connected to Fabric */
  LifecycleMethodDeploy = 1,
  /** LifecycleMethodEnterProcess - Will be called by the runtime when receiving an instance's data for the first time */
  LifecycleMethodEnterProcess = 2,
  /** LifecycleMethodExitProcess - Will be called by the runtime when before exiting or after processing an instance's stop call */
  LifecycleMethodExitProcess = 3,
  LifecycleMethodStart = 4,
  LifecycleMethodStop = 5,
  UNRECOGNIZED = -1,
}

export function lifecycleMethodFromJSON(object: any): LifecycleMethod {
  switch (object) {
    case 0:
    case "LifecycleMethodNull":
      return LifecycleMethod.LifecycleMethodNull;
    case 1:
    case "LifecycleMethodDeploy":
      return LifecycleMethod.LifecycleMethodDeploy;
    case 2:
    case "LifecycleMethodEnterProcess":
      return LifecycleMethod.LifecycleMethodEnterProcess;
    case 3:
    case "LifecycleMethodExitProcess":
      return LifecycleMethod.LifecycleMethodExitProcess;
    case 4:
    case "LifecycleMethodStart":
      return LifecycleMethod.LifecycleMethodStart;
    case 5:
    case "LifecycleMethodStop":
      return LifecycleMethod.LifecycleMethodStop;
    case -1:
    case "UNRECOGNIZED":
    default:
      return LifecycleMethod.UNRECOGNIZED;
  }
}

export function lifecycleMethodToJSON(object: LifecycleMethod): string {
  switch (object) {
    case LifecycleMethod.LifecycleMethodNull:
      return "LifecycleMethodNull";
    case LifecycleMethod.LifecycleMethodDeploy:
      return "LifecycleMethodDeploy";
    case LifecycleMethod.LifecycleMethodEnterProcess:
      return "LifecycleMethodEnterProcess";
    case LifecycleMethod.LifecycleMethodExitProcess:
      return "LifecycleMethodExitProcess";
    case LifecycleMethod.LifecycleMethodStart:
      return "LifecycleMethodStart";
    case LifecycleMethod.LifecycleMethodStop:
      return "LifecycleMethodStop";
    case LifecycleMethod.UNRECOGNIZED:
    default:
      return "UNRECOGNIZED";
  }
}

export interface ExecutionsCreateRequest {
  execution: PerperExecution | undefined;
  instance: PerperInstance | undefined;
  delegate: string;
  arguments: Any[];
}

export interface ExecutionsListenRequest {
  instanceFilter: PerperInstance | undefined;
  delegate: string;
  localToData: boolean;
}

export interface ExecutionsListenResponse {
  execution: PerperExecution | undefined;
  instance: PerperInstance | undefined;
  delegate: string;
  deleted: boolean;
  arguments: Any[];
  startOfStream: boolean;
}

export interface ExecutionsReserveRequest {
  execution: PerperExecution | undefined;
  workgroup: string;
}

export interface ExecutionsListenAndReserveRequest {
  reserveNext: number;
  filter: ExecutionsListenRequest | undefined;
  workgroup: string;
}

export interface ExecutionsCompleteRequest {
  execution: PerperExecution | undefined;
  results: Any[];
  error: PerperError | undefined;
}

export interface ExecutionsGetResultRequest {
  execution: PerperExecution | undefined;
  waitForCreation: boolean;
}

export interface ExecutionsGetResultResponse {
  deleted: boolean;
  results: Any[];
  error: PerperError | undefined;
}

export interface ExecutionsDeleteRequest {
  execution: PerperExecution | undefined;
}

export interface InstancesCreateAndStartRequest {
  instance: PerperInstance | undefined;
  arguments: Any[];
}

export interface InstancesStopAndDeleteRequest {
  instance: PerperInstance | undefined;
  stopParameters: Any[];
}

function createBaseExecutionsCreateRequest(): ExecutionsCreateRequest {
  return { execution: undefined, instance: undefined, delegate: "", arguments: [] };
}

export const ExecutionsCreateRequest = {
  encode(message: ExecutionsCreateRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== undefined) {
      PerperExecution.encode(message.execution, writer.uint32(10).fork()).ldelim();
    }
    if (message.instance !== undefined) {
      PerperInstance.encode(message.instance, writer.uint32(18).fork()).ldelim();
    }
    if (message.delegate !== "") {
      writer.uint32(26).string(message.delegate);
    }
    for (const v of message.arguments) {
      Any.encode(v!, writer.uint32(50).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsCreateRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsCreateRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.execution = PerperExecution.decode(reader, reader.uint32());
          break;
        case 2:
          message.instance = PerperInstance.decode(reader, reader.uint32());
          break;
        case 3:
          message.delegate = reader.string();
          break;
        case 6:
          message.arguments.push(Any.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsCreateRequest {
    return {
      execution: isSet(object.execution) ? PerperExecution.fromJSON(object.execution) : undefined,
      instance: isSet(object.instance) ? PerperInstance.fromJSON(object.instance) : undefined,
      delegate: isSet(object.delegate) ? String(object.delegate) : "",
      arguments: Array.isArray(object?.arguments) ? object.arguments.map((e: any) => Any.fromJSON(e)) : [],
    };
  },

  toJSON(message: ExecutionsCreateRequest): unknown {
    const obj: any = {};
    message.execution !== undefined &&
      (obj.execution = message.execution ? PerperExecution.toJSON(message.execution) : undefined);
    message.instance !== undefined &&
      (obj.instance = message.instance ? PerperInstance.toJSON(message.instance) : undefined);
    message.delegate !== undefined && (obj.delegate = message.delegate);
    if (message.arguments) {
      obj.arguments = message.arguments.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.arguments = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsCreateRequest>, I>>(object: I): ExecutionsCreateRequest {
    const message = createBaseExecutionsCreateRequest();
    message.execution = (object.execution !== undefined && object.execution !== null)
      ? PerperExecution.fromPartial(object.execution)
      : undefined;
    message.instance = (object.instance !== undefined && object.instance !== null)
      ? PerperInstance.fromPartial(object.instance)
      : undefined;
    message.delegate = object.delegate ?? "";
    message.arguments = object.arguments?.map((e) => Any.fromPartial(e)) || [];
    return message;
  },
};

function createBaseExecutionsListenRequest(): ExecutionsListenRequest {
  return { instanceFilter: undefined, delegate: "", localToData: false };
}

export const ExecutionsListenRequest = {
  encode(message: ExecutionsListenRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.instanceFilter !== undefined) {
      PerperInstance.encode(message.instanceFilter, writer.uint32(18).fork()).ldelim();
    }
    if (message.delegate !== "") {
      writer.uint32(26).string(message.delegate);
    }
    if (message.localToData === true) {
      writer.uint32(808).bool(message.localToData);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsListenRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsListenRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 2:
          message.instanceFilter = PerperInstance.decode(reader, reader.uint32());
          break;
        case 3:
          message.delegate = reader.string();
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

  fromJSON(object: any): ExecutionsListenRequest {
    return {
      instanceFilter: isSet(object.instanceFilter) ? PerperInstance.fromJSON(object.instanceFilter) : undefined,
      delegate: isSet(object.delegate) ? String(object.delegate) : "",
      localToData: isSet(object.localToData) ? Boolean(object.localToData) : false,
    };
  },

  toJSON(message: ExecutionsListenRequest): unknown {
    const obj: any = {};
    message.instanceFilter !== undefined &&
      (obj.instanceFilter = message.instanceFilter ? PerperInstance.toJSON(message.instanceFilter) : undefined);
    message.delegate !== undefined && (obj.delegate = message.delegate);
    message.localToData !== undefined && (obj.localToData = message.localToData);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsListenRequest>, I>>(object: I): ExecutionsListenRequest {
    const message = createBaseExecutionsListenRequest();
    message.instanceFilter = (object.instanceFilter !== undefined && object.instanceFilter !== null)
      ? PerperInstance.fromPartial(object.instanceFilter)
      : undefined;
    message.delegate = object.delegate ?? "";
    message.localToData = object.localToData ?? false;
    return message;
  },
};

function createBaseExecutionsListenResponse(): ExecutionsListenResponse {
  return {
    execution: undefined,
    instance: undefined,
    delegate: "",
    deleted: false,
    arguments: [],
    startOfStream: false,
  };
}

export const ExecutionsListenResponse = {
  encode(message: ExecutionsListenResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== undefined) {
      PerperExecution.encode(message.execution, writer.uint32(10).fork()).ldelim();
    }
    if (message.instance !== undefined) {
      PerperInstance.encode(message.instance, writer.uint32(18).fork()).ldelim();
    }
    if (message.delegate !== "") {
      writer.uint32(34).string(message.delegate);
    }
    if (message.deleted === true) {
      writer.uint32(40).bool(message.deleted);
    }
    for (const v of message.arguments) {
      Any.encode(v!, writer.uint32(50).fork()).ldelim();
    }
    if (message.startOfStream === true) {
      writer.uint32(800).bool(message.startOfStream);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsListenResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsListenResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.execution = PerperExecution.decode(reader, reader.uint32());
          break;
        case 2:
          message.instance = PerperInstance.decode(reader, reader.uint32());
          break;
        case 4:
          message.delegate = reader.string();
          break;
        case 5:
          message.deleted = reader.bool();
          break;
        case 6:
          message.arguments.push(Any.decode(reader, reader.uint32()));
          break;
        case 100:
          message.startOfStream = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsListenResponse {
    return {
      execution: isSet(object.execution) ? PerperExecution.fromJSON(object.execution) : undefined,
      instance: isSet(object.instance) ? PerperInstance.fromJSON(object.instance) : undefined,
      delegate: isSet(object.delegate) ? String(object.delegate) : "",
      deleted: isSet(object.deleted) ? Boolean(object.deleted) : false,
      arguments: Array.isArray(object?.arguments) ? object.arguments.map((e: any) => Any.fromJSON(e)) : [],
      startOfStream: isSet(object.startOfStream) ? Boolean(object.startOfStream) : false,
    };
  },

  toJSON(message: ExecutionsListenResponse): unknown {
    const obj: any = {};
    message.execution !== undefined &&
      (obj.execution = message.execution ? PerperExecution.toJSON(message.execution) : undefined);
    message.instance !== undefined &&
      (obj.instance = message.instance ? PerperInstance.toJSON(message.instance) : undefined);
    message.delegate !== undefined && (obj.delegate = message.delegate);
    message.deleted !== undefined && (obj.deleted = message.deleted);
    if (message.arguments) {
      obj.arguments = message.arguments.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.arguments = [];
    }
    message.startOfStream !== undefined && (obj.startOfStream = message.startOfStream);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsListenResponse>, I>>(object: I): ExecutionsListenResponse {
    const message = createBaseExecutionsListenResponse();
    message.execution = (object.execution !== undefined && object.execution !== null)
      ? PerperExecution.fromPartial(object.execution)
      : undefined;
    message.instance = (object.instance !== undefined && object.instance !== null)
      ? PerperInstance.fromPartial(object.instance)
      : undefined;
    message.delegate = object.delegate ?? "";
    message.deleted = object.deleted ?? false;
    message.arguments = object.arguments?.map((e) => Any.fromPartial(e)) || [];
    message.startOfStream = object.startOfStream ?? false;
    return message;
  },
};

function createBaseExecutionsReserveRequest(): ExecutionsReserveRequest {
  return { execution: undefined, workgroup: "" };
}

export const ExecutionsReserveRequest = {
  encode(message: ExecutionsReserveRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== undefined) {
      PerperExecution.encode(message.execution, writer.uint32(10).fork()).ldelim();
    }
    if (message.workgroup !== "") {
      writer.uint32(82).string(message.workgroup);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsReserveRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsReserveRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.execution = PerperExecution.decode(reader, reader.uint32());
          break;
        case 10:
          message.workgroup = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsReserveRequest {
    return {
      execution: isSet(object.execution) ? PerperExecution.fromJSON(object.execution) : undefined,
      workgroup: isSet(object.workgroup) ? String(object.workgroup) : "",
    };
  },

  toJSON(message: ExecutionsReserveRequest): unknown {
    const obj: any = {};
    message.execution !== undefined &&
      (obj.execution = message.execution ? PerperExecution.toJSON(message.execution) : undefined);
    message.workgroup !== undefined && (obj.workgroup = message.workgroup);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsReserveRequest>, I>>(object: I): ExecutionsReserveRequest {
    const message = createBaseExecutionsReserveRequest();
    message.execution = (object.execution !== undefined && object.execution !== null)
      ? PerperExecution.fromPartial(object.execution)
      : undefined;
    message.workgroup = object.workgroup ?? "";
    return message;
  },
};

function createBaseExecutionsListenAndReserveRequest(): ExecutionsListenAndReserveRequest {
  return { reserveNext: 0, filter: undefined, workgroup: "" };
}

export const ExecutionsListenAndReserveRequest = {
  encode(message: ExecutionsListenAndReserveRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.reserveNext !== 0) {
      writer.uint32(8).uint64(message.reserveNext);
    }
    if (message.filter !== undefined) {
      ExecutionsListenRequest.encode(message.filter, writer.uint32(18).fork()).ldelim();
    }
    if (message.workgroup !== "") {
      writer.uint32(82).string(message.workgroup);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsListenAndReserveRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsListenAndReserveRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.reserveNext = longToNumber(reader.uint64() as Long);
          break;
        case 2:
          message.filter = ExecutionsListenRequest.decode(reader, reader.uint32());
          break;
        case 10:
          message.workgroup = reader.string();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsListenAndReserveRequest {
    return {
      reserveNext: isSet(object.reserveNext) ? Number(object.reserveNext) : 0,
      filter: isSet(object.filter) ? ExecutionsListenRequest.fromJSON(object.filter) : undefined,
      workgroup: isSet(object.workgroup) ? String(object.workgroup) : "",
    };
  },

  toJSON(message: ExecutionsListenAndReserveRequest): unknown {
    const obj: any = {};
    message.reserveNext !== undefined && (obj.reserveNext = Math.round(message.reserveNext));
    message.filter !== undefined &&
      (obj.filter = message.filter ? ExecutionsListenRequest.toJSON(message.filter) : undefined);
    message.workgroup !== undefined && (obj.workgroup = message.workgroup);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsListenAndReserveRequest>, I>>(
    object: I,
  ): ExecutionsListenAndReserveRequest {
    const message = createBaseExecutionsListenAndReserveRequest();
    message.reserveNext = object.reserveNext ?? 0;
    message.filter = (object.filter !== undefined && object.filter !== null)
      ? ExecutionsListenRequest.fromPartial(object.filter)
      : undefined;
    message.workgroup = object.workgroup ?? "";
    return message;
  },
};

function createBaseExecutionsCompleteRequest(): ExecutionsCompleteRequest {
  return { execution: undefined, results: [], error: undefined };
}

export const ExecutionsCompleteRequest = {
  encode(message: ExecutionsCompleteRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== undefined) {
      PerperExecution.encode(message.execution, writer.uint32(10).fork()).ldelim();
    }
    for (const v of message.results) {
      Any.encode(v!, writer.uint32(50).fork()).ldelim();
    }
    if (message.error !== undefined) {
      PerperError.encode(message.error, writer.uint32(58).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsCompleteRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsCompleteRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.execution = PerperExecution.decode(reader, reader.uint32());
          break;
        case 6:
          message.results.push(Any.decode(reader, reader.uint32()));
          break;
        case 7:
          message.error = PerperError.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsCompleteRequest {
    return {
      execution: isSet(object.execution) ? PerperExecution.fromJSON(object.execution) : undefined,
      results: Array.isArray(object?.results) ? object.results.map((e: any) => Any.fromJSON(e)) : [],
      error: isSet(object.error) ? PerperError.fromJSON(object.error) : undefined,
    };
  },

  toJSON(message: ExecutionsCompleteRequest): unknown {
    const obj: any = {};
    message.execution !== undefined &&
      (obj.execution = message.execution ? PerperExecution.toJSON(message.execution) : undefined);
    if (message.results) {
      obj.results = message.results.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.results = [];
    }
    message.error !== undefined && (obj.error = message.error ? PerperError.toJSON(message.error) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsCompleteRequest>, I>>(object: I): ExecutionsCompleteRequest {
    const message = createBaseExecutionsCompleteRequest();
    message.execution = (object.execution !== undefined && object.execution !== null)
      ? PerperExecution.fromPartial(object.execution)
      : undefined;
    message.results = object.results?.map((e) => Any.fromPartial(e)) || [];
    message.error = (object.error !== undefined && object.error !== null)
      ? PerperError.fromPartial(object.error)
      : undefined;
    return message;
  },
};

function createBaseExecutionsGetResultRequest(): ExecutionsGetResultRequest {
  return { execution: undefined, waitForCreation: false };
}

export const ExecutionsGetResultRequest = {
  encode(message: ExecutionsGetResultRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== undefined) {
      PerperExecution.encode(message.execution, writer.uint32(10).fork()).ldelim();
    }
    if (message.waitForCreation === true) {
      writer.uint32(40).bool(message.waitForCreation);
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsGetResultRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsGetResultRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.execution = PerperExecution.decode(reader, reader.uint32());
          break;
        case 5:
          message.waitForCreation = reader.bool();
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsGetResultRequest {
    return {
      execution: isSet(object.execution) ? PerperExecution.fromJSON(object.execution) : undefined,
      waitForCreation: isSet(object.waitForCreation) ? Boolean(object.waitForCreation) : false,
    };
  },

  toJSON(message: ExecutionsGetResultRequest): unknown {
    const obj: any = {};
    message.execution !== undefined &&
      (obj.execution = message.execution ? PerperExecution.toJSON(message.execution) : undefined);
    message.waitForCreation !== undefined && (obj.waitForCreation = message.waitForCreation);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsGetResultRequest>, I>>(object: I): ExecutionsGetResultRequest {
    const message = createBaseExecutionsGetResultRequest();
    message.execution = (object.execution !== undefined && object.execution !== null)
      ? PerperExecution.fromPartial(object.execution)
      : undefined;
    message.waitForCreation = object.waitForCreation ?? false;
    return message;
  },
};

function createBaseExecutionsGetResultResponse(): ExecutionsGetResultResponse {
  return { deleted: false, results: [], error: undefined };
}

export const ExecutionsGetResultResponse = {
  encode(message: ExecutionsGetResultResponse, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.deleted === true) {
      writer.uint32(40).bool(message.deleted);
    }
    for (const v of message.results) {
      Any.encode(v!, writer.uint32(50).fork()).ldelim();
    }
    if (message.error !== undefined) {
      PerperError.encode(message.error, writer.uint32(58).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsGetResultResponse {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsGetResultResponse();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 5:
          message.deleted = reader.bool();
          break;
        case 6:
          message.results.push(Any.decode(reader, reader.uint32()));
          break;
        case 7:
          message.error = PerperError.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsGetResultResponse {
    return {
      deleted: isSet(object.deleted) ? Boolean(object.deleted) : false,
      results: Array.isArray(object?.results) ? object.results.map((e: any) => Any.fromJSON(e)) : [],
      error: isSet(object.error) ? PerperError.fromJSON(object.error) : undefined,
    };
  },

  toJSON(message: ExecutionsGetResultResponse): unknown {
    const obj: any = {};
    message.deleted !== undefined && (obj.deleted = message.deleted);
    if (message.results) {
      obj.results = message.results.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.results = [];
    }
    message.error !== undefined && (obj.error = message.error ? PerperError.toJSON(message.error) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsGetResultResponse>, I>>(object: I): ExecutionsGetResultResponse {
    const message = createBaseExecutionsGetResultResponse();
    message.deleted = object.deleted ?? false;
    message.results = object.results?.map((e) => Any.fromPartial(e)) || [];
    message.error = (object.error !== undefined && object.error !== null)
      ? PerperError.fromPartial(object.error)
      : undefined;
    return message;
  },
};

function createBaseExecutionsDeleteRequest(): ExecutionsDeleteRequest {
  return { execution: undefined };
}

export const ExecutionsDeleteRequest = {
  encode(message: ExecutionsDeleteRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.execution !== undefined) {
      PerperExecution.encode(message.execution, writer.uint32(10).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): ExecutionsDeleteRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseExecutionsDeleteRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 1:
          message.execution = PerperExecution.decode(reader, reader.uint32());
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): ExecutionsDeleteRequest {
    return { execution: isSet(object.execution) ? PerperExecution.fromJSON(object.execution) : undefined };
  },

  toJSON(message: ExecutionsDeleteRequest): unknown {
    const obj: any = {};
    message.execution !== undefined &&
      (obj.execution = message.execution ? PerperExecution.toJSON(message.execution) : undefined);
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<ExecutionsDeleteRequest>, I>>(object: I): ExecutionsDeleteRequest {
    const message = createBaseExecutionsDeleteRequest();
    message.execution = (object.execution !== undefined && object.execution !== null)
      ? PerperExecution.fromPartial(object.execution)
      : undefined;
    return message;
  },
};

function createBaseInstancesCreateAndStartRequest(): InstancesCreateAndStartRequest {
  return { instance: undefined, arguments: [] };
}

export const InstancesCreateAndStartRequest = {
  encode(message: InstancesCreateAndStartRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.instance !== undefined) {
      PerperInstance.encode(message.instance, writer.uint32(18).fork()).ldelim();
    }
    for (const v of message.arguments) {
      Any.encode(v!, writer.uint32(50).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): InstancesCreateAndStartRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseInstancesCreateAndStartRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 2:
          message.instance = PerperInstance.decode(reader, reader.uint32());
          break;
        case 6:
          message.arguments.push(Any.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): InstancesCreateAndStartRequest {
    return {
      instance: isSet(object.instance) ? PerperInstance.fromJSON(object.instance) : undefined,
      arguments: Array.isArray(object?.arguments) ? object.arguments.map((e: any) => Any.fromJSON(e)) : [],
    };
  },

  toJSON(message: InstancesCreateAndStartRequest): unknown {
    const obj: any = {};
    message.instance !== undefined &&
      (obj.instance = message.instance ? PerperInstance.toJSON(message.instance) : undefined);
    if (message.arguments) {
      obj.arguments = message.arguments.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.arguments = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<InstancesCreateAndStartRequest>, I>>(
    object: I,
  ): InstancesCreateAndStartRequest {
    const message = createBaseInstancesCreateAndStartRequest();
    message.instance = (object.instance !== undefined && object.instance !== null)
      ? PerperInstance.fromPartial(object.instance)
      : undefined;
    message.arguments = object.arguments?.map((e) => Any.fromPartial(e)) || [];
    return message;
  },
};

function createBaseInstancesStopAndDeleteRequest(): InstancesStopAndDeleteRequest {
  return { instance: undefined, stopParameters: [] };
}

export const InstancesStopAndDeleteRequest = {
  encode(message: InstancesStopAndDeleteRequest, writer: _m0.Writer = _m0.Writer.create()): _m0.Writer {
    if (message.instance !== undefined) {
      PerperInstance.encode(message.instance, writer.uint32(18).fork()).ldelim();
    }
    for (const v of message.stopParameters) {
      Any.encode(v!, writer.uint32(50).fork()).ldelim();
    }
    return writer;
  },

  decode(input: _m0.Reader | Uint8Array, length?: number): InstancesStopAndDeleteRequest {
    const reader = input instanceof _m0.Reader ? input : new _m0.Reader(input);
    let end = length === undefined ? reader.len : reader.pos + length;
    const message = createBaseInstancesStopAndDeleteRequest();
    while (reader.pos < end) {
      const tag = reader.uint32();
      switch (tag >>> 3) {
        case 2:
          message.instance = PerperInstance.decode(reader, reader.uint32());
          break;
        case 6:
          message.stopParameters.push(Any.decode(reader, reader.uint32()));
          break;
        default:
          reader.skipType(tag & 7);
          break;
      }
    }
    return message;
  },

  fromJSON(object: any): InstancesStopAndDeleteRequest {
    return {
      instance: isSet(object.instance) ? PerperInstance.fromJSON(object.instance) : undefined,
      stopParameters: Array.isArray(object?.stopParameters)
        ? object.stopParameters.map((e: any) => Any.fromJSON(e))
        : [],
    };
  },

  toJSON(message: InstancesStopAndDeleteRequest): unknown {
    const obj: any = {};
    message.instance !== undefined &&
      (obj.instance = message.instance ? PerperInstance.toJSON(message.instance) : undefined);
    if (message.stopParameters) {
      obj.stopParameters = message.stopParameters.map((e) => e ? Any.toJSON(e) : undefined);
    } else {
      obj.stopParameters = [];
    }
    return obj;
  },

  fromPartial<I extends Exact<DeepPartial<InstancesStopAndDeleteRequest>, I>>(
    object: I,
  ): InstancesStopAndDeleteRequest {
    const message = createBaseInstancesStopAndDeleteRequest();
    message.instance = (object.instance !== undefined && object.instance !== null)
      ? PerperInstance.fromPartial(object.instance)
      : undefined;
    message.stopParameters = object.stopParameters?.map((e) => Any.fromPartial(e)) || [];
    return message;
  },
};

export interface FabricExecutions {
  Create(request: DeepPartial<ExecutionsCreateRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  Listen(request: DeepPartial<ExecutionsListenRequest>, metadata?: grpc.Metadata): Observable<ExecutionsListenResponse>;
  Reserve(request: DeepPartial<ExecutionsReserveRequest>, metadata?: grpc.Metadata): Observable<Empty>;
  ListenAndReserve(
    request: Observable<DeepPartial<ExecutionsListenAndReserveRequest>>,
    metadata?: grpc.Metadata,
  ): Observable<ExecutionsListenResponse>;
  Complete(request: DeepPartial<ExecutionsCompleteRequest>, metadata?: grpc.Metadata): Promise<Empty>;
  GetResult(
    request: DeepPartial<ExecutionsGetResultRequest>,
    metadata?: grpc.Metadata,
  ): Promise<ExecutionsGetResultResponse>;
  Delete(request: DeepPartial<ExecutionsDeleteRequest>, metadata?: grpc.Metadata): Promise<Empty>;
}

export class FabricExecutionsClientImpl implements FabricExecutions {
  private readonly rpc: Rpc;

  constructor(rpc: Rpc) {
    this.rpc = rpc;
    this.Create = this.Create.bind(this);
    this.Listen = this.Listen.bind(this);
    this.Reserve = this.Reserve.bind(this);
    this.ListenAndReserve = this.ListenAndReserve.bind(this);
    this.Complete = this.Complete.bind(this);
    this.GetResult = this.GetResult.bind(this);
    this.Delete = this.Delete.bind(this);
  }

  Create(request: DeepPartial<ExecutionsCreateRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricExecutionsCreateDesc, ExecutionsCreateRequest.fromPartial(request), metadata);
  }

  Listen(
    request: DeepPartial<ExecutionsListenRequest>,
    metadata?: grpc.Metadata,
  ): Observable<ExecutionsListenResponse> {
    return this.rpc.invoke(FabricExecutionsListenDesc, ExecutionsListenRequest.fromPartial(request), metadata);
  }

  Reserve(request: DeepPartial<ExecutionsReserveRequest>, metadata?: grpc.Metadata): Observable<Empty> {
    return this.rpc.invoke(FabricExecutionsReserveDesc, ExecutionsReserveRequest.fromPartial(request), metadata);
  }

  ListenAndReserve(
    request: Observable<DeepPartial<ExecutionsListenAndReserveRequest>>,
    metadata?: grpc.Metadata,
  ): Observable<ExecutionsListenResponse> {
    throw new Error("ts-proto does not yet support client streaming!");
  }

  Complete(request: DeepPartial<ExecutionsCompleteRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricExecutionsCompleteDesc, ExecutionsCompleteRequest.fromPartial(request), metadata);
  }

  GetResult(
    request: DeepPartial<ExecutionsGetResultRequest>,
    metadata?: grpc.Metadata,
  ): Promise<ExecutionsGetResultResponse> {
    return this.rpc.unary(FabricExecutionsGetResultDesc, ExecutionsGetResultRequest.fromPartial(request), metadata);
  }

  Delete(request: DeepPartial<ExecutionsDeleteRequest>, metadata?: grpc.Metadata): Promise<Empty> {
    return this.rpc.unary(FabricExecutionsDeleteDesc, ExecutionsDeleteRequest.fromPartial(request), metadata);
  }
}

export const FabricExecutionsDesc = { serviceName: "perper.grpc2.FabricExecutions" };

export const FabricExecutionsCreateDesc: UnaryMethodDefinitionish = {
  methodName: "Create",
  service: FabricExecutionsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ExecutionsCreateRequest.encode(this).finish();
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

export const FabricExecutionsListenDesc: UnaryMethodDefinitionish = {
  methodName: "Listen",
  service: FabricExecutionsDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return ExecutionsListenRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = ExecutionsListenResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricExecutionsReserveDesc: UnaryMethodDefinitionish = {
  methodName: "Reserve",
  service: FabricExecutionsDesc,
  requestStream: false,
  responseStream: true,
  requestType: {
    serializeBinary() {
      return ExecutionsReserveRequest.encode(this).finish();
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

export const FabricExecutionsCompleteDesc: UnaryMethodDefinitionish = {
  methodName: "Complete",
  service: FabricExecutionsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ExecutionsCompleteRequest.encode(this).finish();
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

export const FabricExecutionsGetResultDesc: UnaryMethodDefinitionish = {
  methodName: "GetResult",
  service: FabricExecutionsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ExecutionsGetResultRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = ExecutionsGetResultResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricExecutionsDeleteDesc: UnaryMethodDefinitionish = {
  methodName: "Delete",
  service: FabricExecutionsDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ExecutionsDeleteRequest.encode(this).finish();
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

export interface FabricInstances {
  CreateAndStart(
    request: DeepPartial<InstancesCreateAndStartRequest>,
    metadata?: grpc.Metadata,
  ): Promise<ExecutionsGetResultResponse>;
  Call(request: DeepPartial<ExecutionsCreateRequest>, metadata?: grpc.Metadata): Promise<ExecutionsGetResultResponse>;
  StopAndDelete(
    request: DeepPartial<InstancesStopAndDeleteRequest>,
    metadata?: grpc.Metadata,
  ): Promise<ExecutionsGetResultResponse>;
}

export class FabricInstancesClientImpl implements FabricInstances {
  private readonly rpc: Rpc;

  constructor(rpc: Rpc) {
    this.rpc = rpc;
    this.CreateAndStart = this.CreateAndStart.bind(this);
    this.Call = this.Call.bind(this);
    this.StopAndDelete = this.StopAndDelete.bind(this);
  }

  CreateAndStart(
    request: DeepPartial<InstancesCreateAndStartRequest>,
    metadata?: grpc.Metadata,
  ): Promise<ExecutionsGetResultResponse> {
    return this.rpc.unary(
      FabricInstancesCreateAndStartDesc,
      InstancesCreateAndStartRequest.fromPartial(request),
      metadata,
    );
  }

  Call(request: DeepPartial<ExecutionsCreateRequest>, metadata?: grpc.Metadata): Promise<ExecutionsGetResultResponse> {
    return this.rpc.unary(FabricInstancesCallDesc, ExecutionsCreateRequest.fromPartial(request), metadata);
  }

  StopAndDelete(
    request: DeepPartial<InstancesStopAndDeleteRequest>,
    metadata?: grpc.Metadata,
  ): Promise<ExecutionsGetResultResponse> {
    return this.rpc.unary(
      FabricInstancesStopAndDeleteDesc,
      InstancesStopAndDeleteRequest.fromPartial(request),
      metadata,
    );
  }
}

export const FabricInstancesDesc = { serviceName: "perper.grpc2.FabricInstances" };

export const FabricInstancesCreateAndStartDesc: UnaryMethodDefinitionish = {
  methodName: "CreateAndStart",
  service: FabricInstancesDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return InstancesCreateAndStartRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = ExecutionsGetResultResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricInstancesCallDesc: UnaryMethodDefinitionish = {
  methodName: "Call",
  service: FabricInstancesDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return ExecutionsCreateRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = ExecutionsGetResultResponse.decode(data);
      return {
        ...value,
        toObject() {
          return value;
        },
      };
    },
  } as any,
};

export const FabricInstancesStopAndDeleteDesc: UnaryMethodDefinitionish = {
  methodName: "StopAndDelete",
  service: FabricInstancesDesc,
  requestStream: false,
  responseStream: false,
  requestType: {
    serializeBinary() {
      return InstancesStopAndDeleteRequest.encode(this).finish();
    },
  } as any,
  responseType: {
    deserializeBinary(data: Uint8Array) {
      const value = ExecutionsGetResultResponse.decode(data);
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
