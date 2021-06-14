using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ds_perper.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Triggers;

namespace ds_perper.Streams
{
    public static class DynamicDataStream
    {
        [FunctionName(nameof(DynamicDataStream))]
        public static async IAsyncEnumerable<dynamic> Run(
            [PerperTrigger] dynamic parameters,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            logger.LogInformation("Started Dynamic Data Stream");
            int? count = parameters.count ?? 100;
            Random rng = new Random();

            for (int i = 0; i < count; i++)
            {
                dynamic data = "as";
                // if (i % 3 == 0)
                // {
                //     data = rng.NextDouble();
                //     if (rng.NextDouble() < 0.6)
                //     {
                //         data = "Beshe null";
                //     } else if (rng.NextDouble() < 0.8)
                //     {
                //         data = "";
                //     }
                // }
                // else if (i % 3 == 1)
                // {
                //     data = "Testify";
                // } else {
                    // data = new SimpleData {
                    //     Name = "Test",
                    //     Priority = i,
                    //     Json = "{ 'test' : 0 }"
                    // };
                // }


                logger.LogInformation("Yielding data...");
                yield return data;
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}