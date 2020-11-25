using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Triggers;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Bindings;

namespace DotNet.FunctionApp
{
    public class Launcher
    {
        private IContext _context;
        public Launcher(IContext context)
        {
            _context = context;
        }

        [FunctionName("DotNet")]
        public async Task RunAsync([PerperTrigger] object parameters, CancellationToken cancellationToken)
        {
            Console.WriteLine("Got a context: {0}!", _context);
            await _context.CallActionAsync("Log", "This was written by an action!");

            var result = await _context.CallFunctionAsync<int>("Called", (8, 3));
            Console.WriteLine("Result from function call is... {0}", result);

            var generator = await _context.StreamFunctionAsync<int>("Generator", 250);

            var processor = await _context.StreamFunctionAsync<int>("Processor", (generator, 2));

            await _context.StreamActionAsync("Consumer", processor); // Final expected sum = 31375 * 2 = 62750
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
        public async IAsyncEnumerable<int> Generator([PerperTrigger] int to)
        {
            for (var i = 0; i <= to; i ++)
            {
                await Task.Delay(10);
                Console.WriteLine("Generating: {0}", i);
                yield return i;
            }
            await Task.Delay(1000000);
        }

        [FunctionName("Processor")]
        [return: Perper()] // HACK!
        public async IAsyncEnumerable<int> Processor([PerperTrigger] (IAsyncEnumerable<int> generator, int x) paramters)
        {
            await foreach (var i in paramters.generator)
            {
                Console.WriteLine("Processing: {0}", i);
                yield return i * paramters.x;
            }
        }

        [FunctionName("Consumer")]
        [return: Perper()] // HACK!
        public async Task Consumer([PerperTrigger] IAsyncEnumerable<int> processor)
        {
            var result = 0;
            await foreach (var i in processor)
            {
                result += i;
                Console.WriteLine("Rolling sum is: {0}", result);
            }
        }
    }
}