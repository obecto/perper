using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace DotNet.FunctionApp
{
    public static class Generator
    {
        [FunctionName("Generator")]
        public static void Run([PerperStreamTrigger] IPerperStreamContext context,
            [PerperStream("count")] IAsyncEnumerable<int> countStream)
        {
           
        }

        static async IAsyncEnumerable<int> Test()
        {
            await Task.Delay(1);
            yield return 1;
        }

        private static Task<IEnumerable<T>> Range<T>(this IAsyncEnumerable<T> collection, Range range)
        {
            return Task.FromResult((IEnumerable<T>) new List<T>());
        }
    }
}