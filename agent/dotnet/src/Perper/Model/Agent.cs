using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Extensions;

namespace Perper.Model
{
    public class Agent : IAgent
    {
        public Agent(PerperAgent rawAgent) => RawAgent = rawAgent;

        public PerperAgent RawAgent { get; }

        public async Task<TResult> CallFunctionAsync<TResult>(string functionName, object[] parameters)
        {
            var results = await CallAsync(functionName, parameters).ConfigureAwait(false);

            if (typeof(ITuple).IsAssignableFrom(typeof(TResult)))
            {
                return (TResult)Activator.CreateInstance(typeof(TResult), results);
            }
            else if (typeof(object[]) == typeof(TResult))
            {
                return (TResult)(object)results;
            }
            else if (results.Length >= 1)
            {
                return (TResult)results[0];
            }
            else
            {
                return default!;
            }
        }

        public async Task CallActionAsync(string actionName, object[] parameters)
        {
            await CallAsync(actionName, parameters).ConfigureAwait(false);
        }

        private async Task<object[]> CallAsync(string @delegate, object[] parameters)
        {
            var call = AsyncLocals.CacheService.GenerateName(@delegate);

            var callNotificationTask = AsyncLocals.NotificationService.GetCallResultNotification(call).ConfigureAwait(false); // HACK: Workaround bug in fabric
            await AsyncLocals.CacheService.CallCreate(call, RawAgent.Agent, RawAgent.Instance, @delegate, AsyncLocals.Agent, AsyncLocals.Instance, parameters).ConfigureAwait(false);
            var (notificationKey, _) = await callNotificationTask;

            var results = await AsyncLocals.CacheService.CallReadResult(call).ConfigureAwait(false);
            await AsyncLocals.NotificationService.ConsumeNotification(notificationKey).ConfigureAwait(false); // TODO: Consume notifications and save state entries in a smarter way
            await AsyncLocals.CacheService.CallRemove(call).ConfigureAwait(false);

            return results;
        }

        public async Task Destroy()
        {
            await AsyncLocals.CacheService.InstanceDestroy(RawAgent.Instance).ConfigureAwait(false);
        }
    }
}