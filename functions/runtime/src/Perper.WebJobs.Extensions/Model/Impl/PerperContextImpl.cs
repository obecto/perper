using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model.Impl
{
    public class PerperContextImpl<T> : IPerperContext<T>
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;

        public T Parameters { get; }

        public PerperContextImpl(FabricService fabric, IIgniteClient ignite, T parameters)
        {
            _fabric = fabric;
            _ignite = ignite;
            
            Parameters = parameters;
        }

        public Task FetchStateAsync(object holder)
        {
            throw new System.NotImplementedException();
        }

        public Task FetchStateAsync(object holder, string name)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateStateAsync(object holder)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateStateAsync(object holder, string name)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAgent> StartAgentAsync(string name, object? parameters = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default)
        {
            throw new System.NotImplementedException();
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<(IStream<TItem>, string)> StreamExternAsync<TItem>(string externName, bool ephemeral = true)
        {
            throw new System.NotImplementedException();
        }

        public Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, bool ephemeral = true)
        {
            throw new System.NotImplementedException();
        }

        public Task<IStream> StreamActionAsync(string actionName, object? parameters = default, bool ephemeral = true)
        {
            throw new System.NotImplementedException();
        }

        public IStream<TItem> DeclareStreamAsync<TItem>(string functionName)
        {
            throw new System.NotImplementedException();
        }

        public Task InitializeStreamAsync(IStream stream, object? parameters = default)
        {
            throw new System.NotImplementedException();
        }
    }
}