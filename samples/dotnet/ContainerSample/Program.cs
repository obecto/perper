using System;

using Perper.Application;
using Perper.Extensions;

var agent = "container-sample";
var instance = PerperStartup.ConfigureInstance();
await using var fabricService = await PerperStartup.EstablishConnection().ConfigureAwait(false);

var startupExecution = await fabricService.GetExecutionsReader(agent, instance, PerperContext.StartupFunctionName).ReadAsync().ConfigureAwait(false);
var startupParameters = await fabricService.ReadExecutionParameters(startupExecution.Execution).ConfigureAwait(false);
await fabricService.WriteExecutionFinished(startupExecution.Execution).ConfigureAwait(false);

var id = Guid.NewGuid();

await foreach (var testExecution in fabricService.GetExecutionsReader(agent, instance, "Test").ReadAllAsync())
{
    await fabricService.WriteExecutionResult(testExecution.Execution, new object[] { id }).ConfigureAwait(false);
}