namespace SimpleAgent
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Perper.WebJobs.Extensions.CustomHandler;

    internal class Program
    {
        private static async Task Main()
        {
            const string agentDelegate = "lean-strategy-agent";

            Environment.SetEnvironmentVariable("PERPER_AGENT_NAME", agentDelegate);
            Environment.SetEnvironmentVariable("PERPER_ROOT_AGENT", agentDelegate);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var perperContext = PerperContext.Instance;

            try
            {
                var startAgentParameters = await perperContext.GetParametersAsync<string>(cancellationTokenSource.Token);
                Console.WriteLine($"Start agent parameters: {startAgentParameters}");

                var functionsTask = Functions.StartAsync(perperContext, cancellationTokenSource.Token);
                await TriggerFunctionsAsync(perperContext);

                Console.ReadKey();
                cancellationTokenSource.Cancel();

                await functionsTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled");
            }
        }

        private static async Task TriggerFunctionsAsync(PerperContext perperContext)
        {
            const int messagesCount = 24;
            const int batchSize = 10;

            var generator = await perperContext.StreamFunctionAsync<string>(Functions.GeneratorFunction, messagesCount);
            var processor = await perperContext.StreamFunctionAsync<string[]>(Functions.BatchFunction, (generator, batchSize));
            await perperContext.StreamActionAsync(Functions.ConsumerFunction, processor);
        }
    }
}