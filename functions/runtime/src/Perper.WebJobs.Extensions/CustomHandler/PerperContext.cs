using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Newtonsoft.Json;
using Perper.WebJobs.Extensions.Model;
using Swan;

namespace Perper.WebJobs.Extensions.CustomHandler
{
    public class PerperContext : IContext
    {
        private static readonly Lazy<PerperContext> LazyInstance = new Lazy<PerperContext>(() => new PerperContext());

        public static PerperContext Instance => LazyInstance.Value;

        private readonly Dictionary<string, Channel<(object, Guid)>> _callParametersChannels;
        private readonly Dictionary<Guid, Channel<object>> _callResultChannels;
        private IContext _contextImplementation;

        private PerperContext()
        {
            _callParametersChannels = new Dictionary<string, Channel<(object, Guid)>>();
            _callResultChannels = new Dictionary<Guid, Channel<object>>();

            var url = $"http://localhost:{Environment.GetEnvironmentVariable("FUNCTIONS_CUSTOMHANDLER_PORT")}/";
            using var server = new WebServer(o => o.WithUrlPrefix(url).WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new ActionModule("/", HttpVerbs.Any, Handler));
            server.RunAsync();
        }

        public Task<TResult> GetParametersAsync<TResult>()
        {
            throw new NotImplementedException();
        }

        public Task SetResultAsync(object result)
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

        public IAgent Agent { get => _contextImplementation.Agent; }

        public Task<(IAgent, TResult)> StartAgentAsync<TResult>(string name, object? parameters = default)
        {
            return _contextImplementation.StartAgentAsync<TResult>(name, parameters);
        }

        public Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default,
            StreamFlags flags = StreamFlags.Ephemeral)
        {
            return _contextImplementation.StreamFunctionAsync<TItem>(functionName, parameters, flags);
        }

        public Task<IStream> StreamActionAsync(string actionName, object? parameters = default, StreamFlags flags = StreamFlags.Ephemeral)
        {
            return _contextImplementation.StreamActionAsync(actionName, parameters, flags);
        }

        public IStream<TItem> DeclareStreamFunction<TItem>(string functionName)
        {
            return _contextImplementation.DeclareStreamFunction<TItem>(functionName);
        }

        public Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default,
            StreamFlags flags = StreamFlags.Ephemeral)
        {
            return _contextImplementation.InitializeStreamFunctionAsync(stream, parameters, flags);
        }

        public Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Ephemeral)
        {
            return _contextImplementation.CreateBlankStreamAsync<TItem>(flags);
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