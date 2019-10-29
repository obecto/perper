# Perper

Stream-based, horizontally scalable framework for asynchronous data processing, built on top of [Apache Ignite](https://ignite.apache.org/) and [Azure Functions](https://azure.microsoft.com/en-us/services/functions/).

## Overview

Perper consists of two building blocks: **Perper Cluster** and **Perper Functions**. Perper Cluster is built on top of Ignite, utilising its data grid, compute grid and clustering capabilities to provide orchestration layer for Perper Functions. Perper Functions is built on top of Azure Functions as Azure Functions Extension and serves as main programming environment in Perper.    

## Getting started

You can use Perper on all major operating systems: Windows, Linux and macOS.

### Prerequisite

Before running this sample, you must have the following:

- Install [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#v2)
- Install [Docker](https://docs.docker.com/install/)

### Create the local function app project

Run the following command from the command line to create a function app project in the PerperFunctionApp folder of the current local directory

```bash
func init PerperFunctionApp
```

When prompted, select a worker runtime - for now only dotnet and python are supported.

After the project is created, use the following command to navigate to the new PerperFunctionApp project folder.

```bash
cd PerperFunctionApp
```

### Enable Perper Functions

...

### Create a function

...

### Run the function locally

Before running the function locally you have to start Perper Cluster in local development mode:

```bash
docker run -t perper-cluster-local --network=host --ipc=host perper/perper-cluster
```

The following command starts the function app. The start command varies, depending on your project language.

#### C#

```bash
func start --build
```

#### Python

```bash
func start
```

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

### Development environment

Perper Cluster and Perper Functions are built from a common code base and therefore use a single common development environment. For now the recommended development environment is Ubuntu 18.04 LTS.

### Prerequisite

Before running this sample, you must have the following:

- Install [.NET Core 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1)
- Install [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#v2)
- Install [Apache Ignite .NET](https://apacheignite-net.readme.io/docs/cross-platform-support)

### Build

...

## License
[MIT](https://github.com/obecto/perper/blob/master/LICENSE)