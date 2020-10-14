using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class DataFrameGenerator
    {
        [FunctionName("DataFrameGenerator")]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("indices")] int[] indices,
            [Perper("output")] IAsyncCollector<Hashtable> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            foreach (var index in indices)
            {
                await output.AddAsync(new Hashtable { { "Index", index }, { "Index2", index } }, cancellationToken);
            }

            var result = await context.CallWorkerAsync<string>("Host.Functions.PythonWorker", new
            {
                data = context.GetStream()
            }, cancellationToken);

            logger.LogInformation($"DataFrameGenerator End: {result}");
        }
    }
}