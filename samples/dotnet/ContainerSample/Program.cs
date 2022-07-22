using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Perper.Application;
using Perper.Model;

using var host = Host.CreateDefaultBuilder().ConfigurePerper().Build();
var perper = host.Services.GetRequiredService<IPerper>();
var perperConfiguration = host.Services.GetRequiredService<IOptions<PerperConfiguration>>().Value;

var executions = perper.Executions;

var startupExecution = await executions.ListenAsync(new PerperExecutionFilter(perperConfiguration.Agent, perperConfiguration.Instance, PerperAgentsExtensions.StartFunctionName)).FirstAsync().ConfigureAwait(false);
var startupArguments = await executions.GetArgumentsAsync(startupExecution.Execution).ConfigureAwait(false);
await executions.WriteResultAsync(startupExecution.Execution).ConfigureAwait(false);

var id = Guid.NewGuid();

await foreach (var testExecution in executions.ListenAsync(new PerperExecutionFilter(perperConfiguration.Agent, perperConfiguration.Instance, "Test")))
{
    await executions.WriteResultAsync(testExecution.Execution, id).ConfigureAwait(false);
}