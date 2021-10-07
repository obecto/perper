using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Extensions
{
    public static class PerperAgentExtensions
    {
        public static async Task<TResult> CallFunctionAsync<TResult>(this PerperAgent agent, string functionName, params object[] parameters)
        {
            var results = await CallAsync(agent, functionName, parameters).ConfigureAwait(false);

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

        public static async Task CallActionAsync(this PerperAgent agent, string actionName, params object[] parameters)
        {
            await CallAsync(agent, actionName, parameters).ConfigureAwait(false);
        }

        private static async Task<object[]> CallAsync(PerperAgent agent, string @delegate, object[] parameters)
        {
            var call = AsyncLocals.CacheService.GenerateName(@delegate);

            var callNotificationTask = AsyncLocals.NotificationService.GetCallResultNotification(call).ConfigureAwait(false); // HACK: Workaround bug in fabric
            await AsyncLocals.CacheService.CallCreate(call, agent.Agent, agent.Instance, @delegate, AsyncLocals.Agent, AsyncLocals.Instance, parameters).ConfigureAwait(false);
            var (notificationKey, _) = await callNotificationTask;

            var results = await AsyncLocals.CacheService.CallReadResult(call).ConfigureAwait(false);
            await AsyncLocals.NotificationService.ConsumeNotification(notificationKey).ConfigureAwait(false); // TODO: Consume notifications and save state entries in a smarter way
            await AsyncLocals.CacheService.CallRemove(call).ConfigureAwait(false);

            return results;
        }

        public static async Task DestroyAsync(this PerperAgent agent)
        {
            await AsyncLocals.CacheService.InstanceDestroy(agent.Instance).ConfigureAwait(false);
        }
    }
}