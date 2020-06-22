using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNet.FunctionApp.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class PythonProcessor
    {
        [FunctionName("PythonProcessor")]
        public static async Task RunAsync([PerperStreamTrigger] PerperStreamContext context,
            [PerperStream("generator")] IAsyncEnumerable<Data> generator,
            [Perper("multiplier")] int multiplier,
            [PerperStream("output")] IAsyncCollector<Data> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            var state = await context.FetchStateAsync<List<int>>() ?? new List<int>();
            await foreach (var data in generator.WithCancellation(cancellationToken))
            {
                var value = data.Value;
                logger.LogInformation($"Processor is processing value: {value}");

                var result = await context.CallWorkerAsync<string>("Host.Functions.PythonWorker", new
                {
                    value = value.ToString()
                }, cancellationToken);
                state.Add(int.Parse(result));
                await context.UpdateStateAsync(state);
                
                await output.AddAsync(new Data {Value = int.Parse(result)}, cancellationToken);
            }
        }
    }
}