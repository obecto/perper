using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Application
{
    public class PerperInstanceLifecycleService
    {
        private readonly ConcurrentDictionary<PerperAgent, (PerperInstanceLifecycleState, TaskCompletionSource)> States = new();
        private readonly ConcurrentDictionary<(string, PerperInstanceLifecycleState), Channel<PerperAgent>> WaitingForChannels = new();

        public async Task WaitForAsync(PerperAgent agent, PerperInstanceLifecycleState state)
        {
            while (true)
            {
                var (currentState, currentSource) = States.GetOrAdd(
                    agent,
                    _ => (PerperInstanceLifecycleState.Uninitialized, new()));

                if (IsStateAfter(currentState, state))
                {
                    return;
                }


                var channel = WaitingForChannels.GetOrAdd((agent.Agent, state), _ => Channel.CreateUnbounded<PerperAgent>());
                await channel.Writer.WriteAsync(agent).ConfigureAwait(false);
                await currentSource.Task.ConfigureAwait(false);
            }
        }

        public IAsyncEnumerable<PerperAgent> ListenWaitingForAsync(string agent, PerperInstanceLifecycleState state, CancellationToken cancellationToken = default)
        {
            return WaitingForChannels.GetOrAdd((agent, state), _ => Channel.CreateUnbounded<PerperAgent>()).Reader.ReadAllAsync(cancellationToken);
        }

        public void TransitionTo(PerperAgent agent, PerperInstanceLifecycleState state)
        {
            States.AddOrUpdate(
                agent,
                _ => (state, new()),
                (_, current) =>
                {
                    var (currentState, currentSource) = current;
                    if (IsStateAfter(currentState, state))
                    {
                        currentSource.TrySetResult();
                        return (state, new());
                    }
                    else
                    {
                        return current;
                    }
                });
        }

        protected virtual bool IsStateAfter(PerperInstanceLifecycleState currentState, PerperInstanceLifecycleState wantedState)
        {
            return currentState < wantedState;
        }
    }
}