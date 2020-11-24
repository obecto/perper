using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Worker
    {
        [FunctionName("Worker")]
        [return: Perper("$return")]
        public static int Run([PerperWorkerTrigger] PerperWorkerContext context,
            [Perper("value")] int value,
            [Perper("multiplier")] int multiplier,
            [Perper("state")] IEnumerable<int> state)
        {
            return state.LastOrDefault() + value * multiplier;
        }
    }
}