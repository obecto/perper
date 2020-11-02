using System;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Streams;

namespace Perper.WebJobs.Extensions.CustomHandler
{
    public class PerperContext : IContext
    {
        private static readonly Lazy<PerperContext> LazyInstance = new Lazy<PerperContext>(() => new PerperContext());

        public static PerperContext Instance => LazyInstance.Value;

        private PerperContext()
        {
        }

        public Task<T> GetParametersAsync<T>()
        {
            throw new NotImplementedException();
        }

        public Task<(T, Guid)> GetCallParametersAsync<T>(string delegateName)
        {
            throw new NotImplementedException();
        }

        public Task SetCallResultAsync(Guid callId, object result)
        {
            throw new NotImplementedException();
        }

        public Task<(T, Guid)> GetStreamParametersAsync<T>(string delegateName)
        {
            throw new NotImplementedException();
        }

        public Task AddStreamOutputAsync(Guid streamId, object output)
        {
            throw new NotImplementedException();
        }

        public Task FetchStateAsync(object holder)
        {
            throw new NotImplementedException();
        }

        public Task UpdateStateAsync(object holder)
        {
            throw new NotImplementedException();
        }

        public Task UpdateStateAsync<T>(string name, T state)
        {
            throw new NotImplementedException();
        }

        public Task UpdateStateAsync<TKey, TVal>(string name, TKey key, TVal value)
        {
            throw new NotImplementedException();
        }

        public Task<IAgent> StartAgentAsync(string name, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<T> CallFunctionAsync<T>(string functionName, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task CallActionAsync(string actionName, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<IStream<T>> StreamFunctionAsync<T>(string functionName, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<IStream> StreamActionAsync(string actionName, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task UpdateStreamAsync(IStream stream, params object[] parameters)
        {
            throw new NotImplementedException();
        }
    }
}