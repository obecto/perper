# Perper
*(This project is under active development)*

Stream-based, horizontally scalable framework for asynchronous data processing, built on top of [Apache Ignite](https://ignite.apache.org/) and [Azure Functions](https://azure.microsoft.com/en-us/services/functions/).

## Overview

Perper consists of two building blocks: Perper Fabric and Perper Functions. Perper Fabric is built on top of Ignite, utilising its data grid, compute grid and clustering capabilities to provide orchestration layer for Perper Functions. Perper Functions is built on top of Azure Functions as Azure Functions Extension and serves as main programming environment in Perper.

## Getting started

You can use Perper on all major operating systems: Windows, Linux and macOS.

### Prerequisite

Before running this sample, you must have the following:

- Install [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#v2)
- Install [.NET Core 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1)
- Install [Docker](https://docs.docker.com/install/)

### Create the local function app project

Run the following command from the command line to create a function app project in the PerperFunctionApp folder of the current local directory

```bash
func init PerperFunctionApp
```

When prompted, select a worker runtime - for now only dotnet and python are fully supported.

After the project is created, use the following command to navigate to the new PerperFunctionApp project folder.

```bash
cd PerperFunctionApp
```

### Enable Perper Functions

In order to use Perper Functions you have to enable Perper Extension for Azure Functions. You can do this with the following code:

```bash
func extensions install -p Perper.WebJobs.Extensions -v 0.1.0 
```

### Create a function

The way to declare a function and its bindings varies, depending on your project language.

#### C#

```csharp
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace PerperFunctionApp
{
    public static class ProcessData
    {
        [FunctionName("ProcessData")]
        [return: Stream]
        public static int Run([StreamTrigger] StreamContext context, [Stream] int data)
        {
            return data * 10;
        }
    }
}
```

#### Python

Before declaring the function, we have to include Python language bindings for Perper in *requirements.txt*:

```
azure-functions
git+git://github.com/obecto/perper.git#subdirectory=python-binding
```
---
*ProcessData/\_\_init\_\_.py*
```python
import logging

import azure.functions as func
import perper.functions as perper


def main(context: perper.StreamContex, data: int):
    return data * 10

```
---
*ProcessData/function.json*
```json
{
  "scriptFile": "__init__.py",
  "bindings": [
    {
      "name": "context",
      "type": "streamTrigger",
      "direction": "in"
    },
    {
      "name": "data",
      "type": "stream",
      "direction": "in"
    },
    {
      "name": "$return",
      "type": "stream",
      "direction": "out"
    }
  ]
}
```

### Run the function locally

Before running the function locally you have to start Perper Fabric in local development mode:

```bash
docker run -t perper-fabric-local --network=host --ipc=host perper/perper-fabric
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

Perper Fabric and Perper Functions are built from a common code base and therefore use a single common development environment. For now the recommended development environment is Ubuntu 18.04 LTS.

### Prerequisite

Before running this sample, you must have the following:

- Install [.NET Core 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1)
- Install [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#v2)
- Install [Apache Ignite .NET](https://apacheignite-net.readme.io/docs/cross-platform-support)

### Build

*(TODO: Add step-by-step instructions on setting up a local development environment.)*

## License
[MIT](https://github.com/obecto/perper/blob/master/LICENSE)
