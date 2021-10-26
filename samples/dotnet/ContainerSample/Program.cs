using System;

using Perper.Application;
using Perper.Extensions;

var agent = "container-sample";
var instance = PerperStartup.GetConfiguredInstance();
await using var fabricService = await PerperStartup.EstablishConnection().ConfigureAwait(false);

var startupExecution = await fabricService.GetExecutionsReader(agent, instance, PerperContext.StartupFunctionName).ReadAsync().ConfigureAwait(false);
var startupParameters = await fabricService.ReadExecutionParameters(startupExecution.ExecutionId).ConfigureAwait(false);
await fabricService.WriteExecutionFinished(startupExecution.ExecutionId).ConfigureAwait(false);

var id = Guid.NewGuid();

await foreach (var testExecution in fabricService.GetExecutionsReader(agent, instance, "Test").ReadAllAsync())
{
    await fabricService.WriteExecutionResult(testExecution.ExecutionId, new object[] { id }).ConfigureAwait(false);
}