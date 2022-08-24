using System;
using System.Threading;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace MultiProcessSample.Calls;

public static class Deploy
{
    public static async Task RunAsync()
    {
        var nonselfCount = 0;
        await Task.WhenAll(Enumerable.Range(0, 6).Select(async x =>
        {
            while (true)
            {
                var responder = await PerperContext.CallAsync<string>("GetProcessName");
                Console.WriteLine("{0} Got response from {1}", Program.ProcessId, responder);
                Program.CountReply();
            }
        }));
    }
}
