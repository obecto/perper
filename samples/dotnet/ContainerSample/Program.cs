using System;
using System.Linq;

using Perper.Application;
using Perper.Extensions;
using Perper.Model;

var (agent, instance) = PerperConnection.ConfigureInstance();
await using var fabricService = await PerperConnection.EstablishConnection().ConfigureAwait(false);
var executions = (IPerperExecutions)fabricService;

var startupExecution = await executions.ListenAsync(new PerperExecutionFilter(agent, instance, PerperContext.StartupFunctionName)).FirstAsync().ConfigureAwait(false);
var startupArguments = await executions.GetArgumentsAsync(startupExecution.Execution).ConfigureAwait(false);
await executions.WriteResultAsync(startupExecution.Execution).ConfigureAwait(false);

var id = Guid.NewGuid();

await foreach (var testExecution in executions.ListenAsync(new PerperExecutionFilter(agent, instance, "Test")))
{
    await executions.WriteResultAsync(testExecution.Execution, id).ConfigureAwait(false);
}