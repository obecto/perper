using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Newtonsoft.Json;
using Swan;

namespace Perper.WebJobs.Extensions.CustomHandler
{
    public class PerperPerperContext
    {
        private static readonly Lazy<PerperPerperContext> LazyInstance = new Lazy<PerperPerperContext>(() => new PerperPerperContext());

        public static PerperPerperContext Instance => LazyInstance.Value;

        private readonly Dictionary<string, Channel<(object, Guid)>> _callParametersChannels;
        private readonly Dictionary<Guid, Channel<object>> _callResultChannels;

        private PerperPerperContext()
        {
            _callParametersChannels = new Dictionary<string, Channel<(object, Guid)>>();
            _callResultChannels = new Dictionary<Guid, Channel<object>>();

            var url = $"http://localhost:{Environment.GetEnvironmentVariable("FUNCTIONS_CUSTOMHANDLER_PORT")}/";
            using var server = new WebServer(o => o.WithUrlPrefix(url).WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new ActionModule("/", HttpVerbs.Any, Handler));
            server.RunAsync();
        }

        public Task<T> GetParametersAsync<T>()
        {
            throw new NotImplementedException();
        }

        public async Task<(TResult, Guid)> GetCallParametersAsync<TResult>(string delegateName)
        {
            var channel = _callParametersChannels.GetOrAdd(delegateName,
                _ => Channel.CreateUnbounded<(object, Guid)>())!;
            var (parameters, callId) = await channel.Reader.ReadAsync();
            return ((TResult) parameters, callId);
        }

        public async Task SetCallResultAsync(Guid callId, object result)
        {
            await _callResultChannels[callId].Writer.WriteAsync(result);
        }

        public Task<(TResult, Guid)> GetStreamParametersAsync<TResult>(string delegateName)
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

        public Task<IAgent> StartAgentAsync(string name, object? parameters = default)
        {
            throw new NotImplementedException();
        }

        public Task<TItem> CallFunctionAsync<TItem>(string functionName, object? parameters = default)
        {
            throw new NotImplementedException();
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            throw new NotImplementedException();
        }

        public Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default)
        {
            throw new NotImplementedException();
        }

        public Task<IStream> StreamActionAsync(string actionName, object? parameters = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateStreamAsync(IStream stream, object? parameters = default)
        {
            throw new NotImplementedException();
        }

        private async Task Handler(IHttpContext context)
        {
            var invocationId = Guid.Parse(context.Request.Headers["X-Azure-Functions-Invocationid"]);
            dynamic payload = JsonConvert.DeserializeObject(await context.GetRequestBodyAsStringAsync())!;
            if (payload.Metadata.triggerAttribute == "PerperModuleTriggerAttribute")
            {

            }
            else if (payload.Metadata.triggerAttribute == "PerperWorkerTriggerAttribute")
            {

            }
        }
    }
}