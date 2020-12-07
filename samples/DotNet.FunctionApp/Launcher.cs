using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace DotNet.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("DotNet")]
        public static async Task RunAsync([PerperTrigger] object parameters, IContext context, IState state, CancellationToken cancellationToken)
        {
            await context.CallActionAsync("Log", "This was written by an action!");
            var dynamicResult = await context.CallFunctionAsync<dynamic>("Dynamic", new { A = 1, B = 3, Message = "This was read from a dynamic object!" });

            Console.WriteLine("{0}", dynamicResult);
            Console.WriteLine("Dynamically-calculated sum was {0}", dynamicResult.Sum);

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

            var generator = await context.StreamFunctionAsync<dynamic>("Generator", new { Count = count });
            // var (generator, generatorName) = await context.CreateBlankStreamAsync<int>();

            var processor = await context.StreamFunctionAsync<int>("Processor", new { Generator = generator, Multiplier = multiplier });
            await context.StreamActionAsync("Consumer", (processor, expectedFinalValue));

            await Task.Delay(1000); // UGLY: Make sure Consumer/Processor are up

            //await context.CallActionAsync("BlankGenerator", (generatorName, count));
        }

        [FunctionName("Log")]
        public static void Log([PerperTrigger] string message)
        {
            Console.WriteLine(message);
        }

        [FunctionName("Dynamic")]
        public static dynamic Dynamic([PerperTrigger] dynamic item)
        {
            Console.WriteLine(item.Message);
            return new { Sum = item.A + item.B };
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

        [FunctionName("BlankGenerator")]
        public static async Task BlankGenerator(
            [PerperTrigger(ParameterExpression = "{\"stream\":0}")] (string _, int to) parameters,
            [Perper] IAsyncCollector<dynamic> output,
            ILogger logger)
        {
            for (var i = 0; i <= parameters.to; i++)
            {
                logger.LogDebug("Generating: {0}", i);
                await output.AddAsync(new { Num = i });
            }
            await output.FlushAsync();
        }

        [FunctionName("Generator")]
        public static async IAsyncEnumerable<dynamic> Generator([PerperTrigger] dynamic parameters, ILogger logger)
        {
            for (var i = 0; i <= parameters.Count; i++)
            {
                logger.LogDebug("Generating: {0}", i);
                yield return new { Num = i };
            }
            await Task.Delay(1000000);
        }

        [FunctionName("Processor")]
        public static async IAsyncEnumerable<int> Processor([PerperTrigger] dynamic paramters, ILogger logger)
        {
            await foreach (var i in (IAsyncEnumerable<dynamic>)paramters.Generator)
            {
                logger.LogDebug($"Processing: {i}");
                yield return i.Num * paramters.Multiplier;
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