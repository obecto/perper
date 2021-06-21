using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Triggers;

namespace Bob.Calls
{
    public class GetRandomNumber
    {
        [FunctionName("GetRandomNumber")]
        public static async Task<int> RunAsync([PerperTrigger] (int min, int max) parameters)
        {
            var random = new Random();
            return random.Next(parameters.min, parameters.max);
        }
    }
}
