import { DeepPartial, Rpc } from "./proto/fabric_pb2";
import { v4 as uuidV4 } from 'uuid';
import { TaskCollection } from "./task_collection";
import { ExecutionsCreateRequest, ExecutionsDeleteRequest, FabricExecutionsClientImpl } from "./proto/grpc2_executions_pb2";
import { FabricStatesDictionaryClientImpl } from "./proto/grpc2_states_pb2";
import { FabricStreamsClientImpl } from "./proto/grpc2_streams_pb2";
import { PerperExecution, PerperInstance } from "./proto/grpc2_model_pb2";
import { Any } from "@bufbuild/protobuf/dist/types/google/protobuf/any_pb";


export class FabricService {
    private readonly rpc: Rpc;
    private readonly taskCollection: TaskCollection;
    private readonly fabricExecutionsClient: FabricExecutionsClientImpl;
    private readonly fabricDictionaryClient: FabricStatesDictionaryClientImpl;
    private readonly fabricStreamsClient: FabricStreamsClientImpl;


    constructor(rpc: Rpc) {
        this.rpc = rpc;
        this.taskCollection = new TaskCollection();
        this.fabricExecutionsClient = new FabricExecutionsClientImpl(this.rpc);
        this.fabricDictionaryClient = new FabricStatesDictionaryClientImpl(this.rpc);
        this.fabricStreamsClient = new FabricStreamsClientImpl(this.rpc);
    }

    public static getCurrentTicks(): number {
        const dt = new Date();
        const t = (dt.getTime() - new Date(1970, 1, 1).getTime()) / 1000;
        return t;
    }

    public static generateName(basename?: string): string {
        return `${basename}-${uuidV4()}`;
    }

    public async stop() {
        await this.taskCollection.cancel();
        await this.taskCollection.wait();
    }

    public async createInstance(agent: string): Promise<PerperInstance> {
        let initInstance: PerperInstance = { instance: agent, agent: "Registry" };
        let execution = await this.createExcetution(initInstance, "Run", []);

        return { instance: execution.execution, agent: agent};
    }

    public async removeInstance(instance: PerperInstance) {
        return await this.removeExecution({ execution: instance.instance });
    }

    public async createExcetution(instance: PerperInstance, delegate: string, parameters: any[]): Promise<PerperExecution> {
        let execution = { execution: FabricService.generateName(delegate) };
        let packed_parameters = [];

        for (let i = 0; i < parameters.length; i++) {
            let packedParameter = Any.pack(parameters[i]);
            packed_parameters.push(packedParameter);
        }

        let executionCreateRequest: DeepPartial<ExecutionsCreateRequest> = {
            execution: execution,
            instance: instance,
            delegate: delegate,
            arguments: packed_parameters
        };

        await this.fabricExecutionsClient.Create(executionCreateRequest);

        return execution;
    }

    public async removeExecution(execution: PerperExecution) {
        let executionDeleteRequest: DeepPartial<ExecutionsDeleteRequest> = { execution: execution };
        await this.fabricExecutionsClient.Delete(executionDeleteRequest);
    }
}