using System;
using System.Threading.Tasks;

using Perper.Extensions;

namespace ContainerUsageSample.Calls
{
    public static class Init
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting container-sample #1");
            var container1 = await PerperContext.StartAgentAsync("container-sample").ConfigureAwait(false);
            Console.WriteLine("Started container-sample #1");

            Console.WriteLine("Starting container-sample #2");
            var container2 = await PerperContext.StartAgentAsync("container-sample").ConfigureAwait(false);
            Console.WriteLine("Started container-sample #2");

            var id1 = await container1.CallAsync<Guid>("Test", new object[] { 1 }).ConfigureAwait(false);
            var id2 = await container2.CallAsync<Guid>("Test", new object[] { 1 }).ConfigureAwait(false);

            for (var i = 0 ; i < 127 ; i++)
            {
                if (((i ^ (i << 2)) & 8) == 0)
                {
                    var r1 = await container1.CallAsync<Guid>("Test", new object[] { 1 }).ConfigureAwait(false);
                    if (r1 != id1)
                    {
                        throw new InvalidOperationException($"Expected to receive {id1} from agent 1, got {r1}");
                    }
                }
                else
                {
                    var r2 = await container2.CallAsync<Guid>("Test", new object[] { 1 }).ConfigureAwait(false);
                    if (r2 != id2)
                    {
                        throw new InvalidOperationException($"Expected to receive {id2} from agent 2, got {r2}");
                    }
                }
            }

            Console.WriteLine("Test passed!");

            await container1.DestroyAsync().ConfigureAwait(false);
            await container2.DestroyAsync().ConfigureAwait(false);

            Console.WriteLine("Both agents destroyed!");
        }
    }
}