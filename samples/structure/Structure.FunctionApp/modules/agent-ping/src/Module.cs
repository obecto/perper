using System;
using System.Threading;
using System.Threading.Tasks;
using AgentPing.Streams;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace AgentPing
{
    public class Module
    {
        [FunctionName(nameof(Module))]
        [return: Perper("$return")]
        public async Task<IPerperStream> StartAsync(
            [PerperModuleTrigger] PerperModuleContext context,
            [Perper("input")] IPerperStream input,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            logger.LogInformation("Started AgentPing module...");

            var runtimeStream = await context.StreamFunctionAsync(typeof(AgentPingRuntimeStream), new {input = input.Subscribe()});
            return runtimeStream;
        }
    }
}