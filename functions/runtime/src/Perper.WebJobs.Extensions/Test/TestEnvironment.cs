using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Test
{
    public class TestEnvironment
    {
        private Dictionary<(string, string), Func<object?, TestInstance, Task<object?>>> functions = new Dictionary<(string, string), Func<object?, TestInstance, Task<object?>>>();
        private Dictionary<(string, string), object?> storedState = new Dictionary<(string, string), object?>();
        private Dictionary<string, Func<object?, ValueTask>> blankStreams = new Dictionary<string, Func<object?, ValueTask>>();

        private PerperBinarySerializer? _serializer;

        public TestEnvironment()
        {
            _serializer = new PerperBinarySerializer(null);
        }

        public TestInstance CreateAgent(string agentDelegate)
        {
            return new TestInstance(agentDelegate, GenerateName(agentDelegate), this);
        }

        private void _RegisterFunction<TParams, TResult>(string agentDelegate, string functionDelegate, Func<TParams, TestInstance, Task<TResult>> function)
        {
            functions[(agentDelegate, functionDelegate)] = async (input, instance) => Serialize(await function(Deserialize<TParams>(input), instance));
        }

        public void RegisterFunction<TParams, TResult>(string agentDelegate, string functionDelegate, Func<TParams, TestInstance, Task<TResult>> function)
            => _RegisterFunction<TParams, TResult>(agentDelegate, functionDelegate, function);

        public void RegisterFunction<TParams, TResult>(string agentDelegate, string functionDelegate, Func<TParams, TestInstance, TResult> function)
            => _RegisterFunction<TParams, TResult>(agentDelegate, functionDelegate, (input, context) => Task.FromResult(function(input, context)));

        public void RegisterFunction<TParams, TResult>(string agentDelegate, string functionDelegate, Func<TParams, Task<TResult>> function)
            => _RegisterFunction<TParams, TResult>(agentDelegate, functionDelegate, (input, _context) => function(input));

        public void RegisterFunction<TParams, TResult>(string agentDelegate, string functionDelegate, Func<TParams, TResult> function)
            => _RegisterFunction<TParams, TResult>(agentDelegate, functionDelegate, (input, _context) => Task.FromResult(function(input)));

        public void RegisterFunction<TResult>(string agentDelegate, string functionDelegate, Func<TestInstance, Task<TResult>> function)
            => _RegisterFunction<object?, TResult>(agentDelegate, functionDelegate, (_input, context) => function(context));

        public void RegisterFunction<TResult>(string agentDelegate, string functionDelegate, Func<TestInstance, TResult> function)
            => _RegisterFunction<object?, TResult>(agentDelegate, functionDelegate, (_input, context) => Task.FromResult(function(context)));

        public void RegisterFunction<TResult>(string agentDelegate, string functionDelegate, Func<Task<TResult>> function)
            => _RegisterFunction<object?, TResult>(agentDelegate, functionDelegate, (_input, _context) => function());

        public void RegisterFunction<TResult>(string agentDelegate, string functionDelegate, Func<TResult> function)
            => _RegisterFunction<object?, TResult>(agentDelegate, functionDelegate, (_input, _context) => Task.FromResult(function()));

        //
        public void RegisterFunction<TParams>(string agentDelegate, string functionDelegate, Func<TParams, TestInstance, Task> function)
            => _RegisterFunction<TParams, object?>(agentDelegate, functionDelegate, async (input, context) => { await function(input, context); return null; });

        public void RegisterFunction<TParams>(string agentDelegate, string functionDelegate, Action<TParams, TestInstance> function)
            => _RegisterFunction<TParams, object?>(agentDelegate, functionDelegate, (input, context) => { function(input, context); return Task.FromResult<object?>(null); });

        public void RegisterFunction<TParams>(string agentDelegate, string functionDelegate, Func<TParams, Task> function)
            => _RegisterFunction<TParams, object?>(agentDelegate, functionDelegate, async (input, _context) => { await function(input); return null; });

        public void RegisterFunction<TParams>(string agentDelegate, string functionDelegate, Action<TParams> function)
            => _RegisterFunction<TParams, object?>(agentDelegate, functionDelegate, (input, _context) => { function(input); return Task.FromResult<object?>(null); });

        public void RegisterFunction(string agentDelegate, string functionDelegate, Func<TestInstance, Task> function)
            => _RegisterFunction<object?, object?>(agentDelegate, functionDelegate, async (_input, context) => { await function(context); return null; });

        public void RegisterFunction(string agentDelegate, string functionDelegate, Action<TestInstance> function)
            => _RegisterFunction<object?, object?>(agentDelegate, functionDelegate, (_input, context) => { function(context); return Task.FromResult<object?>(null); });

        public void RegisterFunction(string agentDelegate, string functionDelegate, Func<Task> function)
            => _RegisterFunction<object?, object?>(agentDelegate, functionDelegate, async (_input, _context) => { await function(); return null; });

        public void RegisterFunction(string agentDelegate, string functionDelegate, Action function)
            => _RegisterFunction<object?, object?>(agentDelegate, functionDelegate, (_input, _context) => { function(); return Task.FromResult<object?>(null); });

        internal async Task<TResult> CallAsync<TParams, TResult>(TestInstance instance, string functionDelegate, TParams parameters)
        {
            var input = Serialize(parameters);
            var function = functions[(instance.AgentDelegate, functionDelegate)];
            var result = await function(input, instance);
            return Deserialize<TResult>(result);
        }

        public IStream<T> CreateStream<T>(IAsyncEnumerable<T> source)
        {
            var streamName = GenerateName();
            return new AsyncEnumerableTestStream<T>(streamName, this, source);
        }

        public IStream<T> CreateStream<T>(IEnumerable<T> source)
        {
#pragma warning disable CS1998
            async IAsyncEnumerable<T> helper()
            {
                foreach (var item in source)
                {
                    yield return item;
                }
            }
#pragma warning restore CS1998

            return CreateStream(helper());
        }

        public (IStream<T>, string) CreateBlankStream<T>()
        {
            var channel = Channel.CreateUnbounded<T>();
            var stream = CreateStream(channel.Reader.ReadAllAsync());
            var name = ((TestStream)stream).StreamName;
            blankStreams.Add(name, (item) => channel.Writer.WriteAsync(Deserialize<T>(item)));
            return (stream, name);
        }

        public async Task WriteToBlankStream<T>(string name, T value)
        {
            var input = Serialize(value);
            await blankStreams[name](input);
        }

        internal object? Serialize<T>(T value)
        {
            return _serializer != null ? _serializer.Serialize(value) : value;
        }

        internal T Deserialize<T>(object? serialized)
        {
            return (T)(_serializer != null ? _serializer.Deserialize(serialized, typeof(T)) : serialized)!;
        }

        internal string GenerateName(string? baseName = null)
        {
            return $"{baseName}-{Guid.NewGuid()}";
        }
    }
}