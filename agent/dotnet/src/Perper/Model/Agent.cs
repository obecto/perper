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
            var call = AsyncLocals.CacheService.GenerateName(functionName);

            var callNotificationTask = AsyncLocals.NotificationService.GetCallResultNotification(call).ConfigureAwait(false); // HACK: Workaround bug in fabric
            await AsyncLocals.CacheService.CallCreate(call, RawAgent.Agent, RawAgent.Instance, functionName, AsyncLocals.Agent, AsyncLocals.Instance, parameters).ConfigureAwait(false);
            var (notificationKey, _) = await callNotificationTask;
            await AsyncLocals.NotificationService.ConsumeNotification(notificationKey).ConfigureAwait(false); // TODO: Consume notifications and save state entries in a smarter way

            var result = await AsyncLocals.CacheService.CallReadResult<TResult>(call).ConfigureAwait(false);

            return result;
        }

        public async Task CallActionAsync(string actionName, object[] parameters)
        {
            var call = AsyncLocals.CacheService.GenerateName(actionName);

            var callNotificationTask = AsyncLocals.NotificationService.GetCallResultNotification(call).ConfigureAwait(false); // HACK: Workaround bug in fabric
            await AsyncLocals.CacheService.CallCreate(call, RawAgent.Agent, RawAgent.Instance, actionName, AsyncLocals.Agent, AsyncLocals.Instance, parameters).ConfigureAwait(false);
            var (notificationKey, _) = await callNotificationTask;
            await AsyncLocals.NotificationService.ConsumeNotification(notificationKey).ConfigureAwait(false); // TODO: Consume notifications and save state entries in a smarter way

            await AsyncLocals.CacheService.CallReadResult(call).ConfigureAwait(false);
        }

        public async Task Destroy()
        {
            await AsyncLocals.CacheService.InstanceDestroy(RawAgent.Instance).ConfigureAwait(false);
        }
    }
}