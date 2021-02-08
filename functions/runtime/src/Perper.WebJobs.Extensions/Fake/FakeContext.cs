using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Fake
{
    public class FakeContext : IContext
    {
        public FakeAgent Agent { get; }
        IAgent IContext.Agent { get => Agent; }

        public FakeContext(FakeAgent agent)
        {
            Agent = agent;
        }

        public FakeContext() : this(new FakeAgent()) {}

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult>(string delegateName, object? parameters = default)
        {
            var agentDelegate = delegateName;
            var callDelegate = delegateName;

            var agent = Agent.CreateAgent(delegateName);
            var result = await agent.CallFunctionAsync<TResult>(callDelegate, parameters);

            return (agent, result);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var asyncEnumerable = await Agent.CallFunctionAsync<IAsyncEnumerable<TItem>>(functionName, parameters);
            return new FakeStream<TItem>(asyncEnumerable);
        }

        public Task<IStream> StreamActionAsync(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var task = Agent.CallActionAsync(functionName, parameters); // Reference to task is lost!

            return Task.FromResult<IStream>(new FakeStream { ExecutionTask = task });
        }

        public IStream<TItem> DeclareStreamFunction<TItem>(string functionName)
        {
            return new DeclaredFakeStream<TItem>() { FunctionName = functionName };
        }

        public async Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamInstance = (DeclaredFakeStream<TItem>)stream;
            if (streamInstance.FunctionName == null)
            {
                throw new InvalidOperationException("Stream is already initialized");
            }

            streamInstance.SetSource(await Agent.CallFunctionAsync<IAsyncEnumerable<TItem>>(streamInstance.FunctionName, parameters));
        }

        public Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Default)
        {
            return Task.FromResult(Agent.CreateBlankStream<TItem>());
        }
    }
}