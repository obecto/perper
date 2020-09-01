using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Structure.Streams;

namespace Structure
{
    public class App
    {
        [FunctionName(nameof(App))]
        public async Task StartAsync(
            [PerperModuleTrigger(RunOnStartup = true)] PerperModuleContext context,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            logger.LogInformation("Started Perper Structure sample application...");

            var pingInputStream = context.DeclareStream(typeof(InputProviderStream));
            var pingOutputStream = await context.StartChildModuleAsync("ping", pingInputStream, cancellationToken);

            var pongInputStream = context.DeclareStream(typeof(InputProviderStream));
            var pongOutputStream = await context.StartChildModuleAsync("pong", pongInputStream, cancellationToken);

            await context.StreamFunctionAsync(pingInputStream, new {input = pongOutputStream});
            await context.StreamFunctionAsync(pongInputStream, new {input = pingOutputStream});

            await context.StreamActionAsync(typeof(EnvironmentMonitorStream),
                new
                {
                    agentPingOutput = pingOutputStream.Subscribe(), 
                    agentPongOutput = pongOutputStream.Subscribe()
                });
        }
    }
}