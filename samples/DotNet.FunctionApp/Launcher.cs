using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace DotNet.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("Launcher")]
        public static async Task Launch([PerperStreamTrigger] IPerperStreamContext<string> context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
        }

        [FunctionName("GenerateData")]
        public static async Task GenerateData([PerperStreamTrigger] IPerperStreamContext<string> context, 
            [PerperStream("data")] IPerperStream<string> data,
            [PerperStream] IAsyncCollector<string> output)
        {
            await output.AddAsync("Hello");
        }
    }
}