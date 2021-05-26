using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;
using Perper.WebJobs.Extensions.Dataflow;
using System.Threading.Tasks.Dataflow;

namespace DotNet.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("DotNet")]
        public static async Task RunAsync([PerperTrigger] object parameters, IContext context, IState state, CancellationToken cancellationToken)
        {
            if (parameters != null)
            {
                var (agent, agentResult) = await context.StartAgentAsync<int>("TestAgent", 42);
                await context.CallActionAsync("Log", $"TestAgent returned {agentResult}");

                var dynamicResult = await context.CallFunctionAsync<dynamic>("Dynamic", new { A = 1, B = 3, Message = "This was read from a dynamic object!" });

                Console.WriteLine("{0}", dynamicResult);
                Console.WriteLine("Dynamically-calculated sum was {0}", dynamicResult.Sum);
                return;
            }

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
        public static void Log([PerperTrigger] string message) => Console.WriteLine(message);

        [FunctionName("Dynamic")]
        public static dynamic Dynamic([PerperTrigger] dynamic item)
        {
            Console.WriteLine(item.Message);
            return new { Sum = item.A + item.B };
        }

        [FunctionName("Called")]
        public static int Called([PerperTrigger] (int a, int b) parameters, CancellationToken cancellationToken) => parameters.a / parameters.b;

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
            yield return null;
            for (var i = 0; i <= parameters.Count; i++)
            {
                logger.LogDebug("Generating: {0}", i);
                yield return new { Num = i };
            }
            await Task.Delay(1000000);
        }

        /*
        private class GeneratorImpl : IAsyncEnumerable<object>
        {
            public int i;
            public int count;

            public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken)
            {
                return new EnumeratorImpl() {Current = null, self = this};
            }

            private class EnumeratorImpl : IAsyncEnumerator<object> {
                public object Current { get; set; }
                public GeneratorImpl self;
                public ValueTask<bool> MoveNextAsync()
                {
                    Current = new { Num = self.i };
                    self.i++;
                    if (self.i <= self.count + 1)
                    {
                        return new ValueTask<bool>(Task.FromResult(true));
                    }
                    return new ValueTask<bool>(Task.FromResult(false));
                }
                public ValueTask DisposeAsync()
                {
                    return new ValueTask(Task.CompletedTask);
                }
            }
        }

        [FunctionName("Generator")]
        public static IAsyncEnumerable<dynamic> Generator([PerperTrigger] dynamic parameters, ILogger logger)
        {
            return new GeneratorImpl() { i = 0, count = parameters.Count };
        }
        */

        [FunctionName("Processor")]
        public static IAsyncEnumerable<int> Processor([PerperTrigger] dynamic paramters, ILogger logger)
        {
            var source = ((IAsyncEnumerable<dynamic>)paramters.Generator).ToDataflow();

            var processor = new TransformBlock<dynamic, int>(input => (int)(input.Num * paramters.Multiplier));

            source.LinkTo(processor, x => x != null);

            return processor.ToAsyncEnumerable();
        }

        [FunctionName("Consumer")]
        public static async Task Consumer([PerperTrigger] (IAsyncEnumerable<int> processor, int expectedFinal) parameters, IStateEntry<int?> consumerState, ILogger logger)
        {
            var lastI = -1;
            await foreach (var i in parameters.processor)
            {
                consumerState.Value = consumerState.Value ?? 10;
                if (i <= lastI) throw new Exception("Incorrect order of received items!");
                lastI = i;

                consumerState.Value += i;
                logger.LogDebug($"Rolling sum is: {consumerState.Value}");

                if (consumerState.Value == parameters.expectedFinal + 10)
                {
                    Console.WriteLine("Consumer finished successfully");
                }
            }
        }
    }
}