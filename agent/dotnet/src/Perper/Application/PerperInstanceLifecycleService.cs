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
        private readonly ConcurrentDictionary<PerperInstance, (PerperInstanceLifecycleState, TaskCompletionSource)> States = new();
        private readonly ConcurrentDictionary<(string, PerperInstanceLifecycleState), Channel<PerperInstance>> WaitingForChannels = new();

        public async Task WaitForAsync(PerperInstance instance, PerperInstanceLifecycleState state)
        {
            while (true)
            {
                var (currentState, currentSource) = States.GetOrAdd(
                    instance,
                    _ => (PerperInstanceLifecycleState.Uninitialized, new()));

                if (IsStateAfter(currentState, state))
                {
                    return;
                }


                var channel = WaitingForChannels.GetOrAdd((instance.Agent, state), _ => Channel.CreateUnbounded<PerperInstance>());
                await channel.Writer.WriteAsync(instance).ConfigureAwait(false);
                await currentSource.Task.ConfigureAwait(false);
            }
        }

        public IAsyncEnumerable<PerperInstance> ListenWaitingForAsync(string agent, PerperInstanceLifecycleState state, CancellationToken cancellationToken = default)
        {
            return WaitingForChannels.GetOrAdd((agent, state), _ => Channel.CreateUnbounded<PerperInstance>()).Reader.ReadAllAsync(cancellationToken);
        }

        public void TransitionTo(PerperInstance instance, PerperInstanceLifecycleState state)
        {
            States.AddOrUpdate(
                instance,
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