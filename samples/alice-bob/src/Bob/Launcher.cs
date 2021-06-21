using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Triggers;

namespace Bob
{
    public class Launcher
    {
        [FunctionName("Bob")]
        public static async Task<string> RunAsync([PerperTrigger] string callerAgentName)
        {
            return $"Hello {callerAgentName}";
        }
    }
}
