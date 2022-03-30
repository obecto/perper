import os
from clr_loader import get_coreclr
from pythonnet import set_runtime
import sys
sys.path.append("/home/nikola/RiderProjects/IgniteTest/IgniteTest/bin/Debug/net5.0/")

rt = get_coreclr("/home/nikola/RiderProjects/IgniteTest/IgniteTest/bin/Debug/net5.0/IgniteTest.runtimeconfig.json")
set_runtime(rt)
import clr

clr.AddReference("IgniteTest")
from IgniteTest import Program

import grpc
from ..protocol import FabricService


def establish_connection():
    ignite_endpoint = os.getenv("APACHE_IGNITE_ENDPOINT", "127.0.0.1:10800")
    grpc_endpoint = os.getenv("PERPER_FABRIC_ENDPOINT", "http://127.0.0.1:40400")
    print(f"APACHE_IGNITE_ENDPOINT: {ignite_endpoint}")
    print(f"PERPER_FABRIC_ENDPOINT: {grpc_endpoint}")

    ignite_utils = Program()
    ignite = ignite_utils.StartIgniteSync()

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
