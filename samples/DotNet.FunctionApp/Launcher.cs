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
        private static DateTime _firstGenerated;
        private static DateTime _lastGenerated;
        private static DateTime _lastProcessed;

        private IContext _context;
        public Launcher(IContext context)
        {
            _context = context;
        }

        [FunctionName("DotNet")]
        public async Task RunAsync([PerperTrigger] object parameters, CancellationToken cancellationToken)
        {
            await _context.CallActionAsync("Log", "This was written by an action!");

            var result = await _context.CallFunctionAsync<int>("Called", (8, 3));
            Console.WriteLine("Result from function call is... {0}", result);

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

        [FunctionName("Generator")]
        [return: Perper()] // HACK!
        public async IAsyncEnumerable<int> Generator([PerperTrigger] int to, ILogger logger)
        {
            _firstGenerated = DateTime.Now;
            for (var i = 0; i <= to; i ++)
            {
                logger.LogDebug("Generating: {0}", i);
                yield return i;
                _lastGenerated = DateTime.Now;
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
                _lastProcessed = DateTime.Now;
            }
        }

        [FunctionName("Consumer")]
        [return: Perper()] // HACK!
        public async Task Consumer([PerperTrigger] (IAsyncEnumerable<int> processor, int expectedFinal) parameters, ILogger logger)
        {
            var result = 0;
            var lastI = -1;
            await foreach (var i in parameters.processor)
            {
                if (i <= lastI) throw new Exception("Incorrect order of received items!");
                lastI = i;

                result += i;
                logger.LogDebug($"Rolling sum is: {result}");

                if (result == parameters.expectedFinal)
                {
                    var _lastConsumed = DateTime.Now;
                    Console.WriteLine("Generate step: {0}", _lastGenerated - _firstGenerated);
                    Console.WriteLine("Process step: {0}", _lastProcessed - _lastGenerated);
                    Console.WriteLine("Consume step: {0}", _lastConsumed - _lastProcessed);
                    Console.WriteLine("Total pipeline: {0}", _lastConsumed - _firstGenerated);
                }
            }
        }
    }
}