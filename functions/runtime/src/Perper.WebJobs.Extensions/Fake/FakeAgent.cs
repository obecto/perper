using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Fake
{
    public class FakeAgent : IAgent
    {
        public string AgentDelegate { get; set; } = "unnamed agent";

        private Dictionary<string, Func<object?, Task<object?>>> functions = new Dictionary<string, Func<object?, Task<object?>>>();
        private Dictionary<string, Func<FakeAgent>> agentConstructors = new Dictionary<string, Func<FakeAgent>>();
        private Dictionary<string, Func<object?, ValueTask>> blankStreams = new Dictionary<string, Func<object?, ValueTask>>();

        public Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default)
        {
            return CallAsync<object?, TResult>(functionName, parameters);
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            return CallFunctionAsync<object?>(actionName, parameters);
        }

        public void RegisterFunction<TParams, TResult>(string functionDelegate, Func<TParams, Task<TResult>> function)
        {
            functions.Add(functionDelegate, async (input) => FakeConfiguration.Serialize(await function(FakeConfiguration.Deserialize<TParams>(input))));
        }

        public void RegisterFunction<TParams, TResult>(string functionDelegate, Func<TParams, TResult> function)
        {
            functions.Add(functionDelegate, (input) => Task.FromResult(FakeConfiguration.Serialize(function(FakeConfiguration.Deserialize<TParams>(input)))));
        }

        public void RegisterFunction<TParams>(string functionDelegate, Func<TParams, Task> function)
        {
            functions.Add(functionDelegate, async (input) => { await function(FakeConfiguration.Deserialize<TParams>(input)); return null; });
        }

        public void RegisterFunction<TParams>(string functionDelegate, Action<TParams> function)
        {
            functions.Add(functionDelegate, (input) => { function(FakeConfiguration.Deserialize<TParams>(input)); return Task.FromResult<object?>(null); });
        }

        public void RegisterAgent(string agentDelegate, Func<FakeAgent> registration)
        {
            agentConstructors.Add(agentDelegate, registration);
        }

        internal async Task<TResult> CallAsync<TParams, TResult>(string functionDelegate, TParams parameters)
        {
            if (!functions.TryGetValue(functionDelegate, out var function))
            {
                throw new IndexOutOfRangeException("Function '" + functionDelegate + "' is not registered on agent '" + AgentDelegate + "'");
            }
            var input = FakeConfiguration.Serialize(parameters);
            var result = await function(input);
            return FakeConfiguration.Deserialize<TResult>(result);
        }

        public FakeAgent CreateAgent(string agentDelegate)
        {
            if (!agentConstructors.TryGetValue(agentDelegate, out var constructor))
            {
                throw new IndexOutOfRangeException("Agent '" + agentDelegate + "' is not registered on agent '" + AgentDelegate + "'");
            }
            return constructor();
        }

        public (IStream<T>, string) CreateBlankStream<T>()
        {
            var channel = Channel.CreateUnbounded<T>();
            var stream = new FakeStream<T>(channel.Reader.ReadAllAsync());
            var name = Guid.NewGuid().ToString();
            blankStreams.Add(name, (item) => channel.Writer.WriteAsync((T)item!));
            return (stream, name);
        }

        public async Task WriteToBlankStream<T>(string name, T value)
        {
            await blankStreams[name](value);
        }
    }
}