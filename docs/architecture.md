# Perper Architecture

<details> <summary> Table of Contents </summary>

* [Perper Fabric](#perper-fabric)
* [Scaler](#scaler)
* [Agent processes](#agent-processes)
  * [.NET agent library](#.net-agent-library)
  * [Python agent library](#python-agent-library)

</details>

Perper consists of two main building blocks: Perper Fabric and Perper Agents, arranged in a hub-and-spoke architecture. All the Agents connect to the Fabric backbone, which serves as an orchestration and storage layer.

![Architecture Diagram](./images/architecture.drawio.png)

## Perper Fabric

Perper Fabric is built on top of [Apache Ignite](https://ignite.apache.org/), utilizing its data grid, compute grid and clustering capabilities to provide an orchestration layer for agents.

Fabric exposes a GRPC interface that complements Ignite's default thin client interface by adding the ability to listen for executions and streams. More details about that are available on the [Fabric Protocol](./protocol.md) page.

In addition, Fabric keeps track of ephemeral streams, removing their data from Ignite once it is stale.

In the future, Fabric would also be responsible for checking and securing capabilities.

Perper Fabric is available as a [docker image](https://hub.docker.com/r/obecto/perper-fabric) and its source code is available in [`../fabric`](../fabric).

## Scaler

The Scaler is a service component that dictates how agents instances are started, run, and stopped. It may allow for different agents' instances to run in separate processes, to be scaled on demand, or to run local to data, based on scaler-specific configuration.

To interact with Perper agents, the scaler exposes an Agent interface whose every Execution represents an Agent Instance.

Currently, there are two implementations of the Scaler: one using Kubernetes to scale Pods and one using Docker Compose to scale Docker containers.

## Agent processes

Agent processes implement the business logic of a Perper application. They are orchestrated by the Scaler and connect to Perper Fabric. A single Agent process can handle Executions linked to one or more Agent Instances, and, in exotic cases, can handle executions related to multiple Agents.

Agent processes are typically implemented using one of the available agent libraries:

### .NET agent library

The .NET agent library lives in [`../agent/dotnet/src/Perper`](../agent/dotnet/src/Perper) and is published as the [`Perper` NuGet package](https://www.nuget.org/packages/Perper). It is split into 3 layers:
* The Protocol layer: Encapsulates a Perper Fabric connection through the `FabricService` class.
  * The Model layer: contains definitions of standard objects from the protocol, that are not required for using Fabric but are basic to working with libraries in other languages.
* The Extensions layer: Contains extension methods that allow using objects from the Model layer without needing to refer to the Protocol layer directly. (It achieves that by using [`AsyncLocal`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1)-s to pass the Perper connection and execution metadata through the C# async context.)
* The Application layer: contains classes for starting up a Perper Agent Process and hooking up methods to handle Executions.

A usage tutorial for the .NET agent library can be found on the [.NET Usage Tutorial](./dotnet-tutorial.md) page

### Python agent library

The Python agent library lives in [`../agent/python`](../agent/python) and is published as the [`Perper` PyPI package](https://pypi.org/project/Perper/). It follows the same general architecture as the .NET agent library.
