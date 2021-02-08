using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using EmbedIO;
using EmbedIO.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;
using Swan;

namespace Perper.WebJobs.Extensions.CustomHandler
{
    public class PerperContext : IContext, IState
    {
        private static readonly Lazy<PerperContext> LazyInstance = new Lazy<PerperContext>(() => new PerperContext());

        public static PerperContext Instance => LazyInstance.Value;

        private readonly IHost _host;
        private readonly Dictionary<string, Channel<(JObject, Guid)>> _callParametersChannels;
        private readonly Dictionary<Guid, Channel<JObject>> _callResultChannels;

        private PerperContext()
        {
            _host = Host.CreateDefaultBuilder().ConfigureServices((_, services) => 
            {
                services.AddScoped(typeof(PerperInstanceData), typeof(PerperInstanceData));

                services.AddScoped(typeof(IContext), typeof(Context));
                services.AddScoped(typeof(IState), typeof(State));

                services.AddOptions<PerperConfig>().Configure<IConfiguration>((perperConfig, configuration) =>
                {
                    configuration.GetSection("Perper").Bind(perperConfig);
                });

                services.AddSingleton<PerperBinarySerializer>();

                services.AddSingleton<FabricService>(services =>
                {
                    var fabric = ActivatorUtilities.CreateInstance<FabricService>(services);
                    fabric.StartAsync(default).Wait();
                    return fabric;
                });

                // NOTE: Due to how Ignite works, we cannot add more type configurations after starting
                // However, during Azure WebJobs startup, we cannot access the assembly containing functions/types
                // Therefore, care must be taken to not resolve IIgniteClient until Azure has loaded the user's assembly...
                services.AddSingleton<IIgniteClient>(services =>
                {
                    var config = services.GetRequiredService<IOptions<PerperConfig>>().Value;
                    var serializer = services.GetRequiredService<PerperBinarySerializer>();

                    var nameMapper = ActivatorUtilities.CreateInstance<PerperNameMapper>(services);
                    nameMapper.InitializeFromAppDomain();

                    var ignite = Ignition.StartClient(new IgniteClientConfiguration
                    {
                        Endpoints = new List<string> { config.FabricHost },
                        BinaryConfiguration = new BinaryConfiguration()
                        {
                            NameMapper = nameMapper,
                            Serializer = serializer,
                            TypeConfigurations = (
                                from type in nameMapper.WellKnownTypes.Keys
                                where !type.IsGenericTypeDefinition
                                select new BinaryTypeConfiguration(type) { Serializer = serializer }
                            ).ToList()
                        }
                    });

                    serializer.SetBinary(ignite.GetBinary());

                    return ignite;
                });

                services.Configure<ServiceProviderOptions>(options =>
                {
                    options.ValidateScopes = true;
                });
            }).Start();

            _callParametersChannels = new Dictionary<string, Channel<(JObject, Guid)>>();
            _callResultChannels = new Dictionary<Guid, Channel<JObject>>();

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

        public async Task<(TResult, Guid)> GetCallParametersAsync<TResult>(string delegateName) where TResult:JObject
        {
            var channel = _callParametersChannels.GetOrAdd(delegateName,
                _ => Channel.CreateUnbounded<(JObject, Guid)>())!;
            var (parameters, callId) = await channel.Reader.ReadAsync();
            return ((TResult)parameters, callId);
        }

        public async Task SetCallResultAsync(Guid callId, object result)
        {
            await _callResultChannels[callId].Writer.WriteAsync((JObject)result);
        }

        public Task<(TResult, Guid)> GetStreamParametersAsync<TResult>(string delegateName)
        {
            throw new NotImplementedException();
        }

        public Task AddStreamOutputAsync(Guid streamId, object output)
        {
            throw new NotImplementedException();
        }

        public IAgent Agent { get => _host.Services.GetService<IContext>()!.Agent; }

        public Task<(IAgent, TResult)> StartAgentAsync<TResult>(string name, object? parameters = default)
        {
            return _host.Services.GetService<IContext>()!.StartAgentAsync<TResult>(name, parameters);
        }

        public Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default,
            StreamFlags flags = StreamFlags.Ephemeral)
        {
            return _host.Services.GetService<IContext>()!.StreamFunctionAsync<TItem>(functionName, parameters, flags);
        }

        public Task<IStream> StreamActionAsync(string actionName, object? parameters = default, StreamFlags flags = StreamFlags.Ephemeral)
        {
            return _host.Services.GetService<IContext>()!.StreamActionAsync(actionName, parameters, flags);
        }

        public IStream<TItem> DeclareStreamFunction<TItem>(string functionName)
        {
            return _host.Services.GetService<IContext>()!.DeclareStreamFunction<TItem>(functionName);
        }

        public Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default,
            StreamFlags flags = StreamFlags.Ephemeral)
        {
            return _host.Services.GetService<IContext>()!.InitializeStreamFunctionAsync(stream, parameters, flags);
        }

        public Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Ephemeral)
        {
            return _host.Services.GetService<IContext>()!.CreateBlankStreamAsync<TItem>(flags);
        }

        public Task<T> GetValue<T>(string key, Func<T> defaultValueFactory)
        {
            return _host.Services.GetService<IState>()!.GetValue(key, defaultValueFactory);
        }

        public Task SetValue<T>(string key, T value)
        {
            return _host.Services.GetService<IState>()!.SetValue(key, value);
        }

        private async Task Handler(IHttpContext context)
        {
            var invocationId = Guid.Parse(context.Request.Headers["X-Azure-Functions-Invocationid"]);
            dynamic payload = JsonConvert.DeserializeObject(await context.GetRequestBodyAsStringAsync())!;
            if (payload.Metadata.triggerAttribute == "PerperTrigger")
            {
                
            }
        }
    }
}