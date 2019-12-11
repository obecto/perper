using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Consumer
    {
        [FunctionName("Consumer")]
        public static void Run([PerperTrigger("Consumer")] IPerperStreamContext context,
            [Perper("processor")] IAsyncEnumerable<int> processor)
        {
        }
    }
}