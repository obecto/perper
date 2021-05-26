using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Fake
{
    public class FakeAgent : IAgent
    {
        private readonly Dictionary<string, Func<object?, Task<object?>>> functions = new Dictionary<string, Func<object?, Task<object?>>>();
        private readonly Dictionary<string, Func<FakeAgent>> agentConstructors = new Dictionary<string, Func<FakeAgent>>();
        private static readonly ConcurrentDictionary<string, ChannelWriter<object?>> blankStreams = new ConcurrentDictionary<string, ChannelWriter<object?>>();

        public Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default) => CallAsync<object?, TResult>(functionName, parameters);

        public Task CallActionAsync(string actionName, object? parameters = default) => CallFunctionAsync<object?>(actionName, parameters);

        public FakeAgent RegisterFunction<TParams, TResult>(string functionDelegate, Func<TParams, Task<TResult>> function)
        {
            functions.Add(functionDelegate, async (input) => FakeConfiguration.Serialize(await function(FakeConfiguration.Deserialize<TParams>(input))));
            return this;
        }

        public FakeAgent RegisterFunction<TParams, TResult>(string functionDelegate, Func<TParams, TResult> function)
        {
            functions.Add(functionDelegate, (input) => Task.FromResult(FakeConfiguration.Serialize(function(FakeConfiguration.Deserialize<TParams>(input)))));
            return this;
        }

        public FakeAgent RegisterFunction<TParams>(string functionDelegate, Func<TParams, Task> function)
        {
            functions.Add(functionDelegate, async (input) => { await function(FakeConfiguration.Deserialize<TParams>(input)); return null; });
            return this;
        }

        public FakeAgent RegisterFunction<TParams>(string functionDelegate, Action<TParams> function)
        {
            functions.Add(functionDelegate, (input) => { function(FakeConfiguration.Deserialize<TParams>(input)); return Task.FromResult<object?>(null); });
            return this;
        }

        public FakeAgent RegisterAgent(string agentDelegate, Func<FakeAgent> registration)
        {
            agentConstructors.Add(agentDelegate, registration);
            return this;
        }

        public FakeAgent CreateAgent(string agentDelegate)
        {
            if (!agentConstructors.TryGetValue(agentDelegate, out var constructor))
            {
                throw new ArgumentOutOfRangeException("Agent '" + agentDelegate + "' is not registered.");
            }
            return constructor();
        }

        internal async Task<TResult> CallAsync<TParams, TResult>(string functionDelegate, TParams parameters)
        {
            if (!functions.TryGetValue(functionDelegate, out var function))
            {
                throw new ArgumentOutOfRangeException("Function '" + functionDelegate + "' is not registered.");
            }
            var input = FakeConfiguration.Serialize(parameters);
            var result = await function(input);
            return FakeConfiguration.Deserialize<TResult>(result);
        }

        public static (IStream<T>, string) CreateBlankStream<T>()
        {
            var channel = Channel.CreateUnbounded<object?>();
            var stream = new FakeStream<T>(channel.Reader.ReadAllAsync().Select(x => FakeConfiguration.Deserialize<T>(x)));
            var name = $"-{Guid.NewGuid()}";
            blankStreams[name] = channel.Writer;
            return (stream, name);
        }

        public static async Task WriteToBlankStream<T>(string name, T value) => await blankStreams[name].WriteAsync(value);

        public static FakeCollector<T> GetBlankStreamCollector<T>(string name) => new FakeCollector<T>(blankStreams[name]);

        public static (FakeCollector<T>, IStream<T>) CreateBlankStreamCollector<T>()
        {
            var (stream, name) = CreateBlankStream<T>();
            return (GetBlankStreamCollector<T>(name), stream);
        }
    }
}