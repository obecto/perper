using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;

namespace Perper.Fabric
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var ignite = Ignition.Start(new IgniteConfiguration
            {
                IgniteHome = "/usr/share/apache-ignite"
            });

            await Task.Delay(1);

            var sequence = GenerateSequence();
            Console.Out.WriteLine("start counter");
            await foreach (var index in sequence)
            {
                if (index > 5)
                {
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10));

            Console.Out.WriteLine("exit counter");
            
        }

        private static async IAsyncEnumerable<int> GenerateSequence()
        {
            var counter = 0;
            await Task.Delay(1);
            yield return counter;
        }
    }
}