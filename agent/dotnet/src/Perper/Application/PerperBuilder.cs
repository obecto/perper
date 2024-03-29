using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;

using Grpc.Net.Client;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Perper.Application.Listeners;
using Perper.Model;
using Perper.Protocol;

using Polly;

namespace Perper.Application
{
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
    public class PerperBuilder : IPerperBuilder
    {
        private readonly IHostBuilder Builder;

        public PerperBuilder(IHostBuilder builder)
        {
            Builder = builder;

            Builder.ConfigureServices(services =>
            {
                services.AddSingleton<IFabricCaster, DefaultFabricCaster>();

                services.AddSingleton<IPerper, FabricService>();
                services.AddSingleton<PerperListenerFilter>();
                services.AddSingleton<PerperInstanceLifecycleService>();

                services.AddScoped<PerperScopeService>();
                services.AddScoped<IPerperContext, PerperContext>();
                services.AddScoped(provider => provider.GetRequiredService<PerperScopeService>().CurrentExecution!);

                services.AddOptions<FabricConfiguration>();

                services.AddOptions<PerperConfiguration>().Configure<ILogger<PerperBuilder>>((configuration, logger) =>
                {
                    configuration.IgniteEndpoint = Environment.GetEnvironmentVariable("APACHE_IGNITE_ENDPOINT") ?? "127.0.0.1:10800";
                    configuration.FabricEndpoint = Environment.GetEnvironmentVariable("PERPER_FABRIC_ENDPOINT") ?? "http://127.0.0.1:40400";
                    configuration.Agent = Environment.GetEnvironmentVariable("X_PERPER_AGENT") ?? null;
                    configuration.Instance = Environment.GetEnvironmentVariable("X_PERPER_INSTANCE") ?? null;

                    logger.LogInformation("APACHE_IGNITE_ENDPOINT: {IgniteEndpoint}", configuration.IgniteEndpoint);
                    logger.LogInformation("PERPER_FABRIC_ENDPOINT: {FabricEndpoint}", configuration.FabricEndpoint);
                    logger.LogInformation("X_PERPER_AGENT: {Agent}", configuration.Agent);
                    logger.LogInformation("X_PERPER_INSTANCE: {Instance}", configuration.Instance);
                });

                services.AddOptions<IgniteClientConfiguration>().Configure(configuration =>
                {
                    configuration.BinaryConfiguration = new BinaryConfiguration
                    {
                        NameMapper = PerperBinaryConfigurations.NameMapper,
                        TypeConfigurations = PerperBinaryConfigurations.TypeConfigurations,
                        ForceTimestamp = true,
                    };
                    configuration.SocketTimeout = TimeSpan.FromSeconds(60);
                });

                services.AddOptions<IgniteClientConfiguration>()
                    .Configure<IOptions<PerperConfiguration>>((igniteConfiguration, perperOptions) =>
                    {
                        igniteConfiguration.Endpoints ??= new List<string>();
                        igniteConfiguration.Endpoints.Add(perperOptions.Value.IgniteEndpoint);
                    });

                services.AddSingleton(provider =>
                {
                    var igniteConfiguration = provider.GetRequiredService<IOptions<IgniteClientConfiguration>>().Value;
                    return Policy
                        .HandleInner<System.Net.Sockets.SocketException>()
                        .WaitAndRetry(10,
                            attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 2)),
                            (exception, timespan) => Console.WriteLine("Failed to connect to Ignite, retrying in {0}s", timespan.TotalSeconds))
                        .Execute(() => Ignition.StartClient(igniteConfiguration));
                });

                services.AddSingleton(provider =>
                {
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    var perperConfiguration = provider.GetRequiredService<IOptions<PerperConfiguration>>().Value;
                    return GrpcChannel.ForAddress(perperConfiguration.FabricEndpoint);
                });
            });
        }

        public IPerperBuilder AddListener(Func<IServiceProvider, IPerperListener> listenerFactory)
        {
            Builder.ConfigureServices(services => services.AddSingleton<IHostedService>((IServiceProvider servicesProvider) =>
            {
                var listener = listenerFactory(servicesProvider);

                servicesProvider.GetService<ILogger<IPerperBuilder>>()?.LogDebug("Registering Listener {Listener}", listener);

                return listener;
            }));
            return this;
        }
    }
}