using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNet.FunctionApp;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Test;

namespace DotNet.FunctionApp.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var env = new TestEnvironment();

            env.RegisterFunction("DotNet", "StatefulSum", (int input, TestInstance instance) => {
                return Launcher.StatefulSum(input, instance, default);
            });

            var agent = env.CreateAgent("DotNet");

            Console.WriteLine(await agent.GetValue<int>("StatefulSum"));

            for (var i = 0; i < 100; i++) {
                await agent.CallActionAsync("StatefulSum", i);
            }

            Console.WriteLine(await agent.GetValue<int>("StatefulSum"));

            var tasks = new List<Task>();
            for (var i = 0; i < 10; i ++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (var i = 0; i < 100; i++)
                    {
                        await agent.CallActionAsync("StatefulSum", i);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine(await agent.GetValue<int>("StatefulSum"));
        }
    }
}
