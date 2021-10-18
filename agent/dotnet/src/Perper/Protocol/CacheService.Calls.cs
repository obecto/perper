using System.Threading.Tasks;

using Perper.Protocol.Instance;

namespace Perper.Protocol
{
    public partial class CacheService
    {
        public Task CallCreate(string call, string agent, string instance, string @delegate, string callerAgent, string caller, object?[] parameters, bool localToData = false)
        {
            var callData = new CallData(agent, instance, @delegate, parameters, callerAgent, caller, localToData);

            return callsCache.PutIfAbsentOrThrowAsync(call, callData);
        }

        public Task CallWriteResult(string call, object?[] result)
        {
            return callsCache.OptimisticUpdateAsync(call, igniteBinary, value => { value.Finished = true; value.Result = result; });
        }

        public Task CallWriteError(string call, string error)
        {
            return callsCache.OptimisticUpdateAsync(call, igniteBinary, value => { value.Finished = true; value.Error = error; });
        }

        public Task CallWriteFinished(string call)
        {
            return callsCache.OptimisticUpdateAsync(call, igniteBinary, value => { value.Finished = true; });
        }

        public async Task<string?> CallReadError(string call)
        {
            return (await callsCache.GetAsync(call).ConfigureAwait(false)).Error;
        }

        public Task CallRemove(string call)
        {
            return callsCache.RemoveAsync(call);
        }

        public async Task<(string?, object?[]?)> CallReadErrorAndResult(string call) // NOTE: should return (string?, TResult?)
        {
            var callData = await callsCache.GetAsync(call).ConfigureAwait(false);

            return (callData.Error, callData.Result);
        }

        public async Task<object?[]> GetCallParameters(string call)
        {
            return (await callsCache.GetAsync(call).ConfigureAwait(false)).Parameters;
        }
    }
}