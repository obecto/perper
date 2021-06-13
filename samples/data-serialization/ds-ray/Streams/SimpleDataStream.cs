using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ds_perper.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Triggers;

namespace ds_perper.Streams
{
    public static class SimpleDataStream
    {
        [FunctionName(nameof(SimpleDataStream))]
        public static async IAsyncEnumerable<SimpleData> Run(
            [PerperTrigger] dynamic parameters,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            logger.LogInformation("Started Simple Data Stream");
            int? count = parameters.count ?? 100;

            for (int i = 0; i < count; i++)
            {
                SimpleData data = new SimpleData {
                    Name = "Test",
                    Priority = i,
                    Json = "{ 'test' : 0 }"
                };

                logger.LogInformation("Yielding data...");
                yield return data;
                //await Task.Delay(10000, cancellationToken);
            }
        }
    }
}