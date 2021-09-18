using System;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;

using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Extensions;

namespace Perper.Protocol.Service
{
    public partial class CacheService
    {
        public Task CallCreate(string call, string agent, string instance, string @delegate, string callerAgent, string caller, object[] parameters, bool localToData = false)
        {
            var callData = CallData.Create(igniteBinary, agent, instance, @delegate, callerAgent, caller, localToData, parameters).Build();

            return callsCache.PutIfAbsentOrThrowAsync(call, callData);
        }

        public Task CallWriteResult<TResult>(string call, TResult result)
        {
            return callsCache.OptimisticUpdateAsync(call, value => CallData.SetResult(value.ToBuilder(), result).Build());
        }

        public Task CallWriteError(string call, string error)
        {
            return callsCache.OptimisticUpdateAsync(call, value => CallData.SetError(value.ToBuilder(), error).Build());
        }

        public Task CallWriteFinished(string call)
        {
            return callsCache.OptimisticUpdateAsync(call, value => CallData.SetFinished(value.ToBuilder()).Build());
        }

        public async Task<string?> CallReadError(string call)
        {
            var callData = await callsCache.GetAsync(call).ConfigureAwait(false);

            return callData.HasField("error") ? callData.GetField<string>("error") : null;
        }

        public async Task<(string?, TResult)> CallReadErrorAndResult<TResult>(string call) // NOTE: should return (string?, TResult?)
        {
            var callData = await callsCache.GetAsync(call).ConfigureAwait(false);

            var error = callData.HasField("error") ? callData.GetField<string>("error") : null;
            TResult result = default!;
            if (callData.HasField("result"))
            {
                var rawResult = callData.GetField<object>("result");
                result = rawResult switch
                {
                    TResult tResult => tResult,
                    IBinaryObject binaryObject => binaryObject.Deserialize<TResult>(),
                    _ => throw new ArgumentException($"Can't convert result from {rawResult?.GetType()?.ToString() ?? "Null"} to {typeof(TResult)}")
                };
            }

            return (error, result);
        }

        public async Task<object[]> GetCallParameters(string call)
        {
            object[] parameters = default!;
            var callData = await callsCache.GetAsync(call).ConfigureAwait(false);

            if (callData.HasField("parameters"))
            {
                parameters = callData.GetField<object[]>("parameters");
            }

            for (var i = 0 ; i < parameters.Length ; i++)
            {
                if (parameters[i] is IBinaryObject binaryObject)
                {
                    parameters[i] = binaryObject.Deserialize<object>();
                }
            }

            return parameters;
        }
    }
}