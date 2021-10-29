import os
import backoff
from pyignite import Client
from pyignite.utils import is_hinted
from pyignite.exceptions import ReconnectError
import grpc
from ..extensions.context_vars import fabric_service, fabric_execution
from ..protocol import FabricService, FabricExecution, TaskCollection


def establish_connection():
    ignite_endpoint = os.getenv("APACHE_IGNITE_ENDPOINT", "127.0.0.1:10800")
    grpc_endpoint = os.getenv("PERPER_FABRIC_ENDPOINT", "http://127.0.0.1:40400")
    print(f"APACHE_IGNITE_ENDPOINT: {ignite_endpoint}")
    print(f"PERPER_FABRIC_ENDPOINT: {grpc_endpoint}")

    (ignite_address, ignite_port) = ignite_endpoint.split(":")
    ignite_port = int(ignite_port)

    ignite = Client()

    @backoff.on_exception(backoff.expo, ReconnectError, on_backoff=(lambda x: print(f"Failed to connect to Ignite, retrying in {x['wait']:0.1f}s")))
    def connect_ignite():
        ignite.connect(ignite_address, ignite_port)

    connect_ignite()

    if grpc_endpoint.startswith("http://"):
        grpc_channel = grpc.aio.insecure_channel(grpc_endpoint[7:])
    elif grpc_endpoint.startswith("https://"):
        grpc_channel = grpc.aio.secure_channel(grpc_endpoint[8:], grpc.ssl_channel_credentials())
    else:  # Fallback
        grpc_channel = grpc.aio.insecure_channel(grpc_endpoint)

    fabric = FabricService(ignite, grpc_channel)

    return fabric


def configure_instance():
    instance = os.getenv("X_PERPER_INSTANCE")
    print(f"X_PERPER_INSTANCE: {instance}")
    return instance
