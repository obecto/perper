# Perper CLI

Crossplatform cli support for Perper.

## Prerequisite
  * [git](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)
  * [Docker](https://docs.docker.com/get-docker/)
  * [Azure Functions Cli v3.x](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=linux%2Ccsharp%2Cbash)
  
### Interaction with Perper Fabric

Usage:

  `perper fabric [command]`

Commands  | Description
--------- | ----------
`logs`    |  Shows logs from Perper Fabric
`restart` |  Finds and restarts Perper Fabric Container
`run`     |  Creates new Fabric container
`stop`    |  Kills and removes a container from the docker host.


### Interact with perper agents

Usage:

  `perper agent [command]`
  
Commands | Description
-------- | ----------
`logs`   | Gets logs of an agent
`run`    | Runs Azure Function

