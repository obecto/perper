using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Config
{
    public static class PerperWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddPerper(this IWebJobsBuilder builder)
        {
            builder.AddExtension<PerperExtensionConfigProvider>().BindOptions<PerperConfig>();

            builder.Services.AddScoped(typeof(PerperInstanceData), typeof(PerperInstanceData));

            builder.Services.AddScoped(typeof(IContext), typeof(Context));
            builder.Services.AddScoped(typeof(IState), typeof(State));
            builder.Services.AddScoped(typeof(IStateEntry<>), typeof(StateEntryDI<>));

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

            // NOTE: Due to how Ignite works, we cannot add more type configurations after starting
            // However, during Azure WebJobs startup, we cannot access the assembly containing functions/types
            // Therefore, care must be taken to not resolve IIgniteClient until Azure has loaded the user's assembly...
            builder.Services.AddSingleton(services =>
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

            builder.Services.Configure<ServiceProviderOptions>(options =>
            {
                options.ValidateScopes = true;
            });

            return builder;
        }
    }
}