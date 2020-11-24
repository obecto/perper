using System;
using System.Threading;
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
            Console.WriteLine("So far, so good: {0}!", _context);
            var result = await _context.CallFunctionAsync<int>("Called", (1, 2));
            Console.WriteLine("And the result is... {0}", result);
            await Task.Yield();
        }

        [FunctionName("Called")]
        [return: Perper()] // HACK!
        public int Called([PerperTrigger] (int a, int b) parameters, CancellationToken cancellationToken)
        {
            Console.WriteLine("Got: {0} and {1}!", parameters.a, parameters.b);
            return parameters.a + parameters.b;
        }
    }
}