using System.Threading.Tasks;
using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Extensions;

namespace Perper.Model
{
    public class Agent : IAgent
    {
        public Agent(PerperAgent rawAgent)
        {
            RawAgent = rawAgent;
        }

        public PerperAgent RawAgent { get; }

        public async Task<TResult> CallFunctionAsync<TResult>(string @delegate, object[] parameters)
        {
            var call = AsyncLocals.CacheService.GenerateName(@delegate);

            await AsyncLocals.CacheService.CallCreate(call, RawAgent.Agent, RawAgent.Instance, @delegate, AsyncLocals.Agent, AsyncLocals.Instance, parameters);

            var (notificationKey, notification) = await AsyncLocals.NotificationService.GetCallResultNotification(call);
            await AsyncLocals.NotificationService.ConsumeNotification(notificationKey); // TODO: Consume notifications and save state entries in a smarter way

            var result = await AsyncLocals.CacheService.CallReadResult<TResult>(call);

            return result;
        }

        public async Task CallActionAsync(string @delegate, object[] parameters)
        {
            var call = AsyncLocals.CacheService.GenerateName(@delegate);

            await AsyncLocals.CacheService.CallCreate(call, RawAgent.Agent, RawAgent.Instance, @delegate, AsyncLocals.Agent, AsyncLocals.Instance, parameters);

            var (notificationKey, notification) = await AsyncLocals.NotificationService.GetCallResultNotification(call);
            await AsyncLocals.NotificationService.ConsumeNotification(notificationKey); // TODO: Consume notifications and save state entries in a smarter way

            await AsyncLocals.CacheService.CallReadResult(call);
        }
    }
}