namespace Perper.WebJobs.Extensions.CustomHandler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache.Affinity;
    using Apache.Ignite.Core.Client;
    using EmbedIO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Linq;
    using Perper.WebJobs.Extensions.Cache;
    using Perper.WebJobs.Extensions.Cache.Notifications;
    using Perper.WebJobs.Extensions.Model;
    using Perper.WebJobs.Extensions.Services;
    using Swan;

    public class PerperContext : IContext, IState
    {
        private static readonly Lazy<PerperContext> LazyInstance = new Lazy<PerperContext>(() => new PerperContext());

        public static PerperContext Instance => LazyInstance.Value;

        private readonly IHost _host;
        private readonly IIgniteClient _igniteClient;
        private readonly FabricService _fabricService;
        private readonly PerperBinarySerializer _perperBinarySerializer;

        /// <summary>
        /// Contains all running tasks for the instance. e.g. listeners, notification consumer task
        /// </summary>
        private readonly List<Task> _tasks;

        /// <summary>
        /// Contains notification tokens ready to be consumed
        /// </summary>
        private readonly Channel<string> _notificationsChannel;

        /// <summary>
        /// Contains notification details per token
        /// </summary>
        private readonly Dictionary<string, (NotificationKey, Notification)> _notificationTokens;

        /// <summary>
        /// Contains parameters channel per delegate
        /// </summary>
        private readonly Dictionary<string, Channel<(JObject, string)>> _delegateParametersChannels;

        private PerperContext()
        {
            AgentDelegate = Environment.GetEnvironmentVariable("PERPER_AGENT_NAME");
            if (string.IsNullOrEmpty(AgentDelegate))
            {
                throw new ArgumentNullException(nameof(AgentDelegate));
            }

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

                    static string? getAffinityKeyFieldName(Type type)
                    {
                        foreach (var prop in type.GetProperties())
                        {
                            if (prop.GetCustomAttribute<AffinityKeyMappedAttribute>() != null)
                            {
                                return prop.Name.ToLower();
                            }
                        }

                        return null;
                    }

                    var ignite = Ignition.StartClient(new IgniteClientConfiguration
                    {
                        Endpoints = new List<string> { config.FabricHost + ":" + config.FabricIgnitePort.ToString() },
                        BinaryConfiguration = new BinaryConfiguration()
                        {
                            NameMapper = nameMapper,
                            Serializer = serializer,
                            TypeConfigurations = (
                                from type in nameMapper.WellKnownTypes.Keys
                                where !type.IsGenericTypeDefinition
                                select new BinaryTypeConfiguration(type)
                                {
                                    Serializer = serializer,
                                    AffinityKeyFieldName = getAffinityKeyFieldName(type)
                                }
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

            _delegateParametersChannels = new Dictionary<string, Channel<(JObject, string)>>();
            _notificationsChannel = Channel.CreateUnbounded<string>();
            _notificationTokens = new Dictionary<string, (NotificationKey, Notification)>();
            _tasks = new List<Task>();

            _perperBinarySerializer = _host.Services.GetRequiredService<PerperBinarySerializer>();
            _igniteClient = _host.Services.GetRequiredService<IIgniteClient>();
            _fabricService = _host.Services.GetRequiredService<FabricService>();

            _tasks.Add(ConsumeCompletedNotificationsAsync());
        }

        public string AgentDelegate { get; }

        public async Task<TResult> GetParametersAsync<TResult>(CancellationToken cancellationToken = default)
        {
            var listenTask = ListenAsync(AgentDelegate);
            _tasks.Add(listenTask);

            var (parameters, token) = await GetParameters<TResult>(AgentDelegate, cancellationToken);
            await _notificationsChannel.Writer.WriteAsync(token);

            return parameters;
        }

        public async Task<(TResult, string)> GetCallParametersAsync<TResult>(string delegateName, CancellationToken cancellationToken = default)
        {
            var listenTask = ListenAsync(delegateName);
            _tasks.Add(listenTask);

            var (parameters, token) = await GetParameters<TResult>(delegateName, cancellationToken);
            return (parameters, token);
        }

        public async Task SetCallResultAsync(string token, object result)
        {
            var callsCache = _igniteClient.GetCache<string, CallData>("calls");

            var callData = await callsCache.GetAsync(token);
            callData.Result = result;
            callData.Finished = true;

            await callsCache.ReplaceAsync(token, callData);

            await _notificationsChannel.Writer.WriteAsync(token);
        }

        public async Task<(TResult, string)> GetStreamParametersAsync<TResult>(string delegateName, CancellationToken cancellationToken = default)
        {
            var listenTask = ListenAsync(delegateName);
            _tasks.Add(listenTask);

            return await GetParameters<TResult>(delegateName, cancellationToken);
        }

        public async Task AddStreamOutputAsync(string streamName, object output)
        {
            var key = DateTime.UtcNow.Ticks - 62135596800000;

            var cache = _igniteClient.GetCache<long, object>(streamName).WithKeepBinary<long, object>();
            var result = await cache.PutIfAbsentAsync(key, _perperBinarySerializer.SerializeRoot(output));

            if (!result)
            {
                throw new Exception($"Duplicate stream item key! {key}");
            }
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

        public Task<IStateEntry<T>> Entry<T>(string key, Func<T> defaultValueFactory)
        {
            return _host.Services.GetService<IState>()!.Entry(key, defaultValueFactory);
        }

        private async Task ConsumeCompletedNotificationsAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var token = await _notificationsChannel.Reader.ReadAsync();
                var (key, notification) = _notificationTokens[token];

                var trigger = JObject.FromObject(notification);
                if (trigger.ContainsKey("Call"))
                {
                    await CompleteCallNotification(trigger);
                }

                await _fabricService.ConsumeNotification(key);

                _notificationTokens.Remove(token);
            }
        }

        private async Task CompleteCallNotification(JObject trigger)
        {
            var call = (string)trigger["Call"]!;
            var callsCache = _igniteClient.GetCache<string, CallData>("calls");
            var callDataResult = await callsCache.TryGetAsync(call);

            if (!callDataResult.Success)
            {
                return;
            }

            var callData = callDataResult.Value;

            callData.Finished = true;
            callData.Error = null;

            await callsCache.ReplaceAsync(call, callData);
        }

        private async Task ListenAsync(string delegateName, CancellationToken cancellationToken = default)
        {
            var taskCollection = new TaskCollection();

            await foreach (var (key, notification) in _fabricService.GetNotifications(delegateName).WithCancellation(cancellationToken))
            {
                taskCollection.Add(() => HandleNotificationAsync(delegateName, key, notification));
            }

            await taskCollection.GetTask();
        }

        private async Task HandleNotificationAsync(string delegateName, NotificationKey key, Notification notification)
        {
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var jObject = JObject.FromObject(notification);
            var channel = GetOrAddCallParametersChannel(delegateName);

            var token = (string)jObject["Stream"]! ?? (string)jObject["Call"]!;

            await channel.Writer.WriteAsync((jObject, token));

            if (notification is StreamTriggerNotification)
            {
                await _fabricService.ConsumeNotification(key);
            }
            else
            {
                _notificationTokens.Add(token, (key, notification));
            }
        }

        private async Task<(TResult, string)> GetParameters<TResult>(string delegateName, CancellationToken cancellationToken = default)
        {
            Channel<(JObject, string)> channel = this.GetOrAddCallParametersChannel(delegateName);
            var (jObject, streamName) = await channel.Reader.ReadAsync(cancellationToken);

            var perperInstanceData = _host.Services.GetRequiredService<PerperInstanceData>();
            await perperInstanceData.SetTriggerValue(jObject);

            var parameters = perperInstanceData.GetParameters(typeof(TResult));

            return ((TResult)parameters, streamName);
        }

        private Channel<(JObject, string)> GetOrAddCallParametersChannel(string name)
        {
            return _delegateParametersChannels.GetOrAdd(name,
                _ => Channel.CreateUnbounded<(JObject, string)>())!;
        }
    }
}