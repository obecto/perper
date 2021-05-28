using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace Bob.Calls
{
    public class GetRandomNumbers
    {
        [FunctionName("GetRandomNumbers")]
        public static async Task<IAsyncEnumerable<int>> RunAsync([PerperTrigger] (int min, int max) parameters,
            IContext context)
        {
            return await context.StreamFunctionAsync<int>("GetRandomNumbersStream", parameters);
        }

        [FunctionName("GetRandomNumbersStream")]
        public static async IAsyncEnumerable<int> GetRandomNumbersStream([PerperTrigger] (int min, int max) parameters)
        {
            var random = new Random();

            while (true)
            {
                yield return random.Next(parameters.min, parameters.max);
                await Task.Delay(1000);
            }
        }
    }
}
