using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Triggers;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("DotNet")]
        public static async Task RunAsync([PerperTrigger] object parameters, IContext context, IState state, CancellationToken cancellationToken)
        {
            await context.CallActionAsync("Log", "This was written by an action!");

            var result = await context.CallFunctionAsync<int>("Called", (8, 3));
            Console.WriteLine("Result from function call is... {0}", result);

            var sum1 = await context.CallFunctionAsync<int>("StatefulSum", 4);
            Console.WriteLine("First sum is: {0}", sum1);
            var sum2 = await context.CallFunctionAsync<int>("StatefulSum", 6);
            Console.WriteLine("Second sum is: {0}", sum2);
            var sum = await state.GetValue("StatefulSum", () => -1);
            Console.WriteLine("Total sum is: {0}", sum);

            var count = 25;
            var multiplier = 7;
            var expectedFinalValue = count * (count + 1) / 2 * multiplier;

            var generator = await context.StreamFunctionAsync<int>("Generator", count);
            var processor = await context.StreamFunctionAsync<int>("Processor", (generator, multiplier));
            await context.StreamActionAsync("Consumer", (processor, expectedFinalValue));
        }

        [FunctionName("Log")]
        public static void Log([PerperTrigger] string message)
        {
            Console.WriteLine(message);
        }

        [FunctionName("Called")]
        public static int Called([PerperTrigger] (int a, int b) parameters, CancellationToken cancellationToken)
        {
            return parameters.a / parameters.b;
        }

        [FunctionName("StatefulSum")]
        public static async Task<int> StatefulSum([PerperTrigger] int n, IState state, CancellationToken cancellationToken)
        {
            var statefulSum = await state.Entry("StatefulSum", () => 0);
            statefulSum.Value += n;
            await statefulSum.Store(); // Would be nice if all entries are automatically stored at function end..
            return statefulSum.Value;
        }

        [FunctionName("Generator")]
        public static async IAsyncEnumerable<int> Generator([PerperTrigger] int to, ILogger logger)
        {
            for (var i = 0; i <= to; i ++)
            {
                logger.LogDebug("Generating: {0}", i);
                yield return i;
            }
            await Task.Delay(1000000);
        }

        [FunctionName("Processor")]
        public static async IAsyncEnumerable<int> Processor([PerperTrigger] (IAsyncEnumerable<int> generator, int x) paramters, ILogger logger)
        {
            await foreach (var i in paramters.generator)
            {
                logger.LogDebug($"Processing: {i}");
                yield return i * paramters.x;
            }
        }

        [FunctionName("Consumer")]
        public static async Task Consumer([PerperTrigger] (IAsyncEnumerable<int> processor, int expectedFinal) parameters, IStateEntry<int> consumerState, ILogger logger)
        {
            var lastI = -1;
            await foreach (var i in parameters.processor)
            {
                if (i <= lastI) throw new Exception("Incorrect order of received items!");
                lastI = i;

                consumerState.Value += i;
                logger.LogDebug($"Rolling sum is: {consumerState.Value}");

                if (consumerState.Value == parameters.expectedFinal)
                {
                    Console.WriteLine("Consumer finished successfully");
                }
            }
        }
    }
}