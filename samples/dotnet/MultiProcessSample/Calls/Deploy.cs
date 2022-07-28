using System;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace MultiProcessSample.Calls;

public static class Deploy
{
    public static async Task RunAsync()
    {
        await Task.WhenAll(Enumerable.Range(0, 1).Select(async x =>
        {
            while (true)
            {
                var responder = await PerperContext.CallAsync<string>("GetProcessName");
                Console.WriteLine("{0} Got response from {1}", Program.ProcessId, responder);
            }
        }));
    }
}
