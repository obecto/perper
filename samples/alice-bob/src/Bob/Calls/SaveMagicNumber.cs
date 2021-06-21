using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Triggers;

namespace Bob.Calls
{
    public class SaveMagicNumber
    {
        [FunctionName("SaveMagicNumber")]
        public static async Task RunAsync([PerperTrigger] int number, ILogger logger)
        {
            logger.LogInformation(number.ToString());
        }
    }
}
