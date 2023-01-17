import { Rpc } from "./proto/fabric_pb2";
import { v4 as uuidV4 } from 'uuid';
import { TaskCollection } from "./task_collection";


export class FabricService {
    private readonly rpc: Rpc;
    private readonly taskCollection: TaskCollection;


    constructor(rpc: Rpc) {
        this.rpc = rpc;
        this.taskCollection = new TaskCollection();
    }

    public static getCurrentTicks(): number {
        const dt = new Date();
        const t = (dt.getTime() - new Date(1970, 1, 1).getTime()) / 1000;
        return t;
    }

    public static generateName(basename?: string): string {
        return `${basename}-${uuidV4()}`;
    }

    public async stop(){
        await this.taskCollection.cancel();
        await this.taskCollection.wait();
    }
}