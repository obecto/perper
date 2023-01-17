import { FabricService } from "../protocol/fabric_service";
import { GrpcWebImpl } from "../protocol/proto/fabric_pb2";


export class Connection {
    public static establish_connection(): FabricService {
        let grpc_endpoint = process.env.PERPER_FABRIC_ENDPOINT ?? "http://127.0.0.1:40400";
        console.log("PERPER_FABRIC_ENDPOINT: " + grpc_endpoint);

        return new FabricService(new GrpcWebImpl(grpc_endpoint, {}));
    }

    public static configure_instance(): string | undefined {
        let instance = process.env.X_PERPER_INSTANCE;
        console.log("X_PERPER_INSTANCE: " + instance);

        return instance;
    }
}