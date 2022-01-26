using System;

using Perper.Application;
using Perper.Extensions;

var (agent, instance) = PerperConnection.ConfigureInstance();
await using var fabricService = await PerperConnection.EstablishConnection().ConfigureAwait(false);

var startupExecution = await fabricService.GetExecutionsReader(agent, instance, PerperContext.StartupFunctionName).ReadAsync().ConfigureAwait(false);
var startupParameters = await fabricService.ReadExecutionParameters(startupExecution.Execution).ConfigureAwait(false);
await fabricService.WriteExecutionFinished(startupExecution.Execution).ConfigureAwait(false);

var id = Guid.NewGuid();

await foreach (var testExecution in fabricService.GetExecutionsReader(agent, instance, "Test").ReadAllAsync())
{
    await fabricService.WriteExecutionResult(testExecution.Execution, new object[] { id }).ConfigureAwait(false);
}