using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace Alice
{
    public class Launcher
    {
        [FunctionName("Alice")]
        public static async Task RunAsync(
            [PerperTrigger] object input,
            IContext context,
            ILogger logger)
        {
            const string agentName = "Bob";
            const string callerAgentNameParameter = "Alice";

            var (agent, greeting) = await context.StartAgentAsync<string>(agentName, callerAgentNameParameter);
            logger.LogInformation(greeting);

            var randomNumber = await agent.CallFunctionAsync<int>("GetRandomNumber", (0, 100));
            logger.LogInformation(randomNumber.ToString());

            await agent.CallActionAsync("SaveMagicNumber", randomNumber);

            var randomNumbersStream = await agent.CallFunctionAsync<IAsyncEnumerable<int>>("GetRandomNumbers", (0, 100));
            await foreach (var number in randomNumbersStream)
            {
                logger.LogInformation(number.ToString());
            }
        }
    }
}
