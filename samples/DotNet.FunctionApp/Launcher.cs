using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Triggers;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Bindings;

namespace DotNet.FunctionApp
{
    public class Launcher
    {
        private IContext _context;
        private IState _state;
        public Launcher(IContext context, IState state)
        {
            _context = context;
            _state = state;
        }

        [FunctionName("DotNet")]
        public async Task RunAsync([PerperTrigger] object parameters, CancellationToken cancellationToken)
        {
            await _context.CallActionAsync("Log", "This was written by an action!");

            var result = await _context.CallFunctionAsync<int>("Called", (8, 3));
            Console.WriteLine("Result from function call is... {0}", result);

            var sum1 = await _context.CallFunctionAsync<int>("StatefulSum", 4);
            Console.WriteLine("First sum is: {0}", sum1);
            var sum2 = await _context.CallFunctionAsync<int>("StatefulSum", 6);
            Console.WriteLine("Second sum is: {0}", sum2);
            var sum = await _state.GetValue("StatefulSum", () => -1);
            Console.WriteLine("Total sum is: {0}", sum);

            var count = 250;
            var multiplier = 7;
            var expectedFinalValue = count * (count + 1) / 2 * multiplier;

            var generator = await _context.StreamFunctionAsync<int>("Generator", count);
            var processor = await _context.StreamFunctionAsync<int>("Processor", (generator, multiplier));
            await _context.StreamActionAsync("Consumer", (processor, expectedFinalValue));
        }

        [FunctionName("Log")]
        public void Log([PerperTrigger] string message)
        {
            Console.WriteLine(message);
        }

        [FunctionName("Called")]
        [return: Perper()] // HACK!
        public int Called([PerperTrigger] (int a, int b) parameters, CancellationToken cancellationToken)
        {
            return parameters.a / parameters.b;
        }

        [FunctionName("StatefulSum")]
        [return: Perper()] // HACK!
        public async Task<int> StatefulSum([PerperTrigger] int n, CancellationToken cancellationToken)
        {
            var statefulSum = await _state.Entry("StatefulSum", () => 0);
            statefulSum.Value += n;
            await statefulSum.Store(); // Would be nice if all entries are automatically stored at function end..
            return statefulSum.Value;
        }

        [FunctionName("Generator")]
        [return: Perper()] // HACK!
        public async IAsyncEnumerable<int> Generator([PerperTrigger] int to, ILogger logger)
        {
            for (var i = 0; i <= to; i ++)
            {
                logger.LogDebug("Generating: {0}", i);
                yield return i;
            }
            await Task.Delay(1000000);
        }

        [FunctionName("Processor")]
        [return: Perper()] // HACK!
        public async IAsyncEnumerable<int> Processor([PerperTrigger] (IAsyncEnumerable<int> generator, int x) paramters, ILogger logger)
        {
            await foreach (var i in paramters.generator)
            {
                logger.LogDebug($"Processing: {i}");
                yield return i * paramters.x;
            }
        }
    }

    public class Consumer
    {
        private IStateEntry<int> _total;
        public Consumer(IContext context, IStateEntry<int> total)
        {
            _total = total;
        }

        [FunctionName("Consumer")]
        [return: Perper()] // HACK!
        public async Task RunAsync([PerperTrigger] (IAsyncEnumerable<int> processor, int expectedFinal) parameters, ILogger logger)
        {
            var lastI = -1;
            await foreach (var i in parameters.processor)
            {
                if (i <= lastI) throw new Exception("Incorrect order of received items!");
                lastI = i;

                _total.Value += i;
                logger.LogDebug($"Rolling sum is: {_total.Value}");

                if (_total.Value == parameters.expectedFinal)
                {
                    Console.WriteLine("Consumer finished successfully");
                }
            }
        }
    }
}