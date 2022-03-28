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
clr.AddReference("Apache.Ignite.Core")
from Apache.Ignite.Core import Ignition
from Apache.Ignite.Core.Client import IgniteClientConfiguration
from Apache.Ignite.Core.Binary import BinaryBasicNameMapper, BinaryConfiguration, BinaryTypeConfiguration
clr.AddReference("System")
from System import TimeSpan, Console, Math
from System.Net.Sockets import SocketException
from System.Threading.Tasks import Task

import grpc
from ..protocol import FabricService


def establish_connection():
    ignite_endpoint = os.getenv("APACHE_IGNITE_ENDPOINT", "127.0.0.1:10800")
    grpc_endpoint = os.getenv("PERPER_FABRIC_ENDPOINT", "http://127.0.0.1:40400")
    print(f"APACHE_IGNITE_ENDPOINT: {ignite_endpoint}")
    print(f"PERPER_FABRIC_ENDPOINT: {grpc_endpoint}")

    ignite_utils = Program()
    ignite = ignite_utils.PollyTestSync()

    ignite = await Policy.HandleInner[SocketException]().WaitAndRetryAsync(10,\
                        lambda attempt: TimeSpan.FromSeconds(Math.Pow(2, attempt - 2)),\
                        lambda exception, timespan: Console.WriteLine.Overloads[str]("Failed to connect to Ignite,\
                        retrying in {0}s", timespan.TotalSeconds))\
                        .ExecuteAsync(lambda: Task.Run(lambda: Ignition.StartClient(igniteConfiguration))).ConfigureAwait(False)

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
