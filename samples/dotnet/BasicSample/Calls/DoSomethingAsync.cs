using System;
using System.Threading.Tasks;

namespace BasicSample.Calls
{
    public class DoSomethingAsync
    {
        public async Task RunAsync(string message)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine("DoSomethingAsync called: " + message);
        }
    }
}