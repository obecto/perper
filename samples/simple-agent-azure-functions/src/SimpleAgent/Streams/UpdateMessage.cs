namespace SimpleAgent.Streams
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Perper.WebJobs.Extensions.Triggers;

    public class UpdateMessage
    {
        [FunctionName(nameof(UpdateMessage))]
        public static async Task<string> RunAsync([PerperTrigger] string message)
        {
            var updatedMessage = message + "_RPC";
            return updatedMessage;
        }
    }
}
