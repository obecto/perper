using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

[assembly: PerperData]

namespace Perper.WebJobs.Extensions.Config
{
    public static class PerperWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddPerper(this IWebJobsBuilder builder)
        {
            builder.Services.AddScoped(typeof(PerperInstanceData), typeof(PerperInstanceData));

            builder.Services.AddScoped(typeof(IContext), typeof(Context));
            builder.Services.AddScoped(typeof(IState), typeof(State));
            builder.Services.AddScoped(typeof(IStateEntry<>), typeof(StateEntryDI<>));

            builder.Services.AddOptions<PerperConfig>().Configure<IConfiguration>((perperConfig, configuration) =>
            {
                configuration.GetSection("Perper").Bind(perperConfig);
            });

            builder.Services.AddSingleton<IBindingProvider>(services => new ServiceBindingProvider(new HashSet<Type>
            {
                typeof(IContext),
                typeof(IState),
                typeof(IStateEntry<>)
            }, services));

            builder.Services.AddSingleton<PerperBinarySerializer>();

            builder.Services.AddSingleton(services =>
            {
                var fabric = ActivatorUtilities.CreateInstance<FabricService>(services);
                fabric.StartAsync(default).Wait();
                return fabric;
            });

            builder.Services.AddSingleton(services =>
            {
                var config = services.GetRequiredService<IOptions<PerperConfig>>();
                var fabricHost = config.Value.FabricHost;

                // NOTE: The check for assembly.GetCustomAttributes<PerperDataAttribute>().Any() means that
                // users need to use [assembly: PerperDataAttribute]. This isn't much of a problem, however,
                // since this is used only for types that cross language boundaries.
                var types = (
                    from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    where assembly.GetCustomAttributes<PerperDataAttribute>().Any()
                    from type in assembly.GetTypes()
                    where type.GetCustomAttributes<PerperDataAttribute>().Any()
                    select type);

                var serializer = services.GetRequiredService<PerperBinarySerializer>();

                var ignite = Ignition.StartClient(new IgniteClientConfiguration
                {
                    Endpoints = new List<string> { fabricHost },
                    BinaryConfiguration = new BinaryConfiguration()
                    {
                        NameMapper = new BinaryBasicNameMapper() { IsSimpleName = true },
                        Serializer = serializer,
                        TypeConfigurations = types.Select(type => new BinaryTypeConfiguration(type)
                        {
                            Serializer = serializer,
                        }).ToList()
                    }
                });

                serializer.SetBinary(ignite.GetBinary());

                return ignite;
            });

            builder.Services.Configure<ServiceProviderOptions>(options =>
            {
                options.ValidateScopes = true;
            });

            builder.AddExtension<PerperExtensionConfigProvider>();

            return builder;
        }
    }
}