using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace DotNet.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("Launcher")]
        public static void Launch([StreamTrigger] StreamContext context)
        {
        }

        [FunctionName("GenerateData")]
        [return: Stream]
        public static int GenerateData([StreamTrigger] StreamContext context, [Stream] int data)
        {
            return 1;
        }
    }
}