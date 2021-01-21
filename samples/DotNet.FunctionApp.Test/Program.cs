using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNet.FunctionApp;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Fake;
using System.Diagnostics;

namespace DotNet.FunctionApp.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var context = new FakeContext();

            /*context.Agent.RegisterAgent("TestAgent", () => {
                var agent = new TestAgent();
                agent.RegisterFunction("TestAgent", (int input) => {
                    Debug.Assert(input == 42);
                    return 78;
                });
                return agent;
            });*/

            context.Agent.RegisterAgent("TestAgent", () => context.Agent);

            context.Agent.RegisterFunction("TestAgent", (int input) => {
                Debug.Assert(input == 42);
                return 78;
            });

            context.Agent.RegisterFunction("Dynamic", (dynamic item) => {
                Debug.Assert((string)item.Message == "This was read from a dynamic object!");
                return new { Sum = item.A + item.B };
            });

            context.Agent.RegisterFunction("Log", (string input) => {
                Debug.Assert(input == $"TestAgent returned 78");
            });

            var state = new FakeState();

            var result = await Launcher.StatefulSum(10, state, default);
            Debug.Assert(result == 10);
            Debug.Assert(state.GetValue<int>("StatefulSum") == 10);

            await Launcher.RunAsync(true, context, state, default);
        }
    }
}
