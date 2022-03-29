using System;
using System.Collections.Generic;

using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;

using Grpc.Net.Client;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Perper.Model;
using Perper.Protocol;

using Polly;

namespace Perper.Application
{
    public class PerperBuilder : IPerperBuilder
    {
        private readonly IHostBuilder Builder;

        public PerperBuilder(IHostBuilder builder)
        {
            Builder = builder;

            Builder.ConfigureServices(services =>
            {
                services.AddHostedService<PerperHandlerService>();
                services.AddSingleton<IFabricCaster, DefaultFabricCaster>();

                services.AddSingleton<IPerper, FabricService>();

                services.AddScoped<PerperScopeService>();
                services.AddScoped<IPerperContext, PerperContext>();
                services.AddScoped(provider => provider.GetRequiredService<PerperScopeService>().CurrentExecution!);

                services.AddOptions<PerperConfiguration>().Configure<ILogger<PerperBuilder>>((configuration, logger) =>
                {
                    configuration.IgniteEndpoint = Environment.GetEnvironmentVariable("APACHE_IGNITE_ENDPOINT") ?? "127.0.0.1:10800";
                    configuration.FabricEndpoint = Environment.GetEnvironmentVariable("PERPER_FABRIC_ENDPOINT") ?? "http://127.0.0.1:40400";
                    configuration.Agent = Environment.GetEnvironmentVariable("X_PERPER_AGENT") ?? null;
                    configuration.Instance = Environment.GetEnvironmentVariable("X_PERPER_INSTANCE") ?? null;

                    logger.LogInformation($"APACHE_IGNITE_ENDPOINT: {configuration.IgniteEndpoint}");
                    logger.LogInformation($"PERPER_FABRIC_ENDPOINT: {configuration.FabricEndpoint}");
                    logger.LogInformation($"X_PERPER_AGENT: {configuration.Agent}");
                    logger.LogInformation($"X_PERPER_INSTANCE: {configuration.Instance}");
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

        public IPerperBuilder AddHandler(IPerperHandler handler)
        {
            Builder.ConfigureServices(services => services.AddSingleton(handler));
            return this;
        }

        public IPerperBuilder AddHandler(Func<IServiceProvider, IPerperHandler> handlerFactory)
        {
            Builder.ConfigureServices(services => services.AddSingleton(handlerFactory));
            return this;
        }
    }
}