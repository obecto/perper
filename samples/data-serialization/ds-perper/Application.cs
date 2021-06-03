using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using ds_perper.Models;
using ds_perper.Streams;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;
using Perper.WebJobs.Extensions.Bindings;

namespace ds_perper
{
    [PerperData]
    public class DynamicParameters{
        public int count;
    }
    public class Application
    {
        [FunctionName("Application")]
        public static async Task StartAsync(
            [PerperTrigger] dynamic parameters,
            IContext context,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            // In the current implementation we use a blank stream and populate it with data via CallActionAsync

            logger.LogInformation("Started SimpleDataSerialization.Application");

            var (testStream, testStreamName) = await context.CreateBlankStreamAsync<dynamic>();
            logger.LogInformation("Stream name: {0}", testStreamName);
            int count = 5000;

            await context.CallActionAsync("BlankGenerator", (testStreamName, count));

            // The following is an option for creating a custom stream with a separate definition

            // var mockDataStream = await context.StreamFunctionAsync<dynamic>(
            //     nameof(DynamicDataStream),
            //     new DynamicParameters{count = 5000},
            //     StreamFlags.None);

            //  await context.StreamActionAsync(
            //     nameof(AppMonitor),
            //     new {
            //         dataStream = mockDataStream
            //     });
        }

        [FunctionName("BlankGenerator")]
        public static async Task BlankGenerator(
            [PerperTrigger(ParameterExpression = "{\"stream\":0}")] PerperTriggerValue parameters,
            [Perper] IAsyncCollector<dynamic> output,
            ILogger logger)
        {
            using(var reader = new StreamReader(@"..\..\Data\google_data.csv"))
            {
                // First we get the collumn names from the csv
                var first_row = reader.ReadLine();
                var column_names = first_row.Split(",");
                logger.LogInformation(string.Join("", column_names));
                // Then in a loop we send the data row by row
                while (!reader.EndOfStream)
                {
                    await Task.Delay(200);
                    var row = reader.ReadLine();
                    var values = row.Split(',');
                    await output.AddAsync(new SimpleData{
                        Name = "Test",
                        Priority = 2,
                        Json = string.Format("{{ \"value\" : {0} }}", values)
                    });
                }
            }
            await output.FlushAsync();
            for (var i = 0; ; i++)
            {
                Random random = new Random();
                int num = random.Next(10,50);
                logger.LogInformation("Generating: {0}, {1}", i, num);
                await Task.Delay(1000);
                await output.AddAsync(new SimpleData{
                        Name = "Test",
                        Priority = i,
                        Json = string.Format("{{ \"value\" : {0} }}", num)
                    });
            }
        }
    }
}