using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Generator
    {
        [FunctionName("Generator")]
        public static void Run([PerperTrigger("Generator")] IPerperStreamContext context,
            [Perper("count")] int count)
        {
        }
    }
}