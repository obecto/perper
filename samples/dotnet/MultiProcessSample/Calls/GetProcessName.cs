using System;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace MultiProcessSample.Calls;

#pragma warning disable CA5394

public static class GetProcessName
{
    private static readonly Random random = new();
    public static async Task<string> RunAsync()
    {
        await Task.Delay(random.Next(100, 1000)).ConfigureAwait(false);
        return $"Name: {Program.ProcessId}";
    }
}
