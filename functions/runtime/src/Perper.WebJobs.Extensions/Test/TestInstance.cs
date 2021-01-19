using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Test
{
    public class TestInstance : IAgent, IContext, IState
    {
        public string AgentDelegate { get; set; }
        public string AgentName { get; set; }

        private readonly TestEnvironment _environment;

        public TestInstance(string agentDelegate, string agentName, TestEnvironment environment)
        {
            AgentDelegate = agentDelegate;
            AgentName = agentName;
            _environment = environment;
        }

        #region State
        public class TestStateEntry<T> : IStateEntry<T>
        {
            public T Value { get; set; } = default(T)!;

            public Func<T> DefaultValueFactory = () => default(T)!;
            public string Name { get; private set; }

            private TestInstance _instance;

            public TestStateEntry(TestInstance instance, string name, Func<T> defaultValueFactory)
            {
                _instance = instance;
                Name = name;
                DefaultValueFactory = defaultValueFactory;
            }

            public async Task Load()
            {
                Value = await _instance.GetValue<T>(Name, DefaultValueFactory);
            }

            public Task Store()
            {
                return _instance.SetValue<T>(Name, Value);
            }
        }

        public ConcurrentDictionary<string, object?> Values = new ConcurrentDictionary<string, object?>();

        public Task<T> GetValue<T>(string key, Func<T> defaultValueFactory)
        {
            return Task.FromResult(_environment.Deserialize<T>(Values.GetOrAdd(key, _k => defaultValueFactory())));
        }

        public Task SetValue<T>(string key, T value)
        {
            Values[key] = _environment.Serialize(value);
            return Task.CompletedTask;
        }

        async Task<IStateEntry<T>> IState.Entry<T>(string key, Func<T> defaultValueFactory)
        {
            var entry = new TestStateEntry<T>(this, key, defaultValueFactory);
            await entry.Load();
            return entry;
        }
        #endregion State

        #region Agent
        public Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default)
        {
            return _environment.CallAsync<object?, TResult>(this, functionName, parameters);
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            return CallFunctionAsync<object?>(actionName, parameters);
        }
        #endregion Agent

        #region Context
        IAgent IContext.Agent => this;

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult>(string delegateName, object? parameters = default)
        {
            var agentDelegate = delegateName;
            var callDelegate = delegateName;

            var agent = _environment.CreateAgent(delegateName);
            var result = await agent.CallFunctionAsync<TResult>(callDelegate, parameters);

            return (agent, result);
        }

        public Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = _environment.GenerateName(functionName);

            return Task.FromResult<IStream<TItem>>(new FunctionTestStream<TItem>(streamName, _environment, this, functionName, parameters));
        }

        public Task<IStream> StreamActionAsync(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = _environment.GenerateName(functionName);

            var task = CallActionAsync(functionName, parameters); // Reference to task is lost!

            return Task.FromResult<IStream>(new TestStream(streamName));
        }

        public IStream<TItem> DeclareStreamFunction<TItem>(string functionName)
        {
            var streamName = _environment.GenerateName(functionName);

            return new FunctionTestStream<TItem>(streamName, _environment, this, functionName, null);
        }

        public Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamInstance = (FunctionTestStream<TItem>)stream;
            if (streamInstance.Parameters != null)
            {
                throw new InvalidOperationException("Stream is already initialized");
            }
            streamInstance.Parameters = parameters;

            return Task.CompletedTask;
        }

        public Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Default)
        {
            return Task.FromResult(_environment.CreateBlankStream<TItem>());
        }
        #endregion Context
    }
}