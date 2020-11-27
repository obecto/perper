using System;
using System.Reflection;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;

namespace Perper.WebJobs.Extensions.Config
{
    public static class PerperWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddPerper(this IWebJobsBuilder builder)
        {
            builder.Services.AddScoped(typeof(IContext), typeof(Context));
            builder.Services.AddScoped(typeof(IState), typeof(State));
            builder.Services.AddScoped(typeof(IStateEntry<>), typeof(StateEntry<>));
            builder.Services.AddScoped(typeof(StreamParameterIndexHelper), typeof(StreamParameterIndexHelper));
            builder.Services.AddSingleton(services => {
                var fabric = ActivatorUtilities.CreateInstance<FabricService>(services);
                fabric.StartAsync(default).Wait();
                return fabric;
            });
            builder.Services.AddSingleton(services => {
                var igniteHost = IPAddress.Loopback.ToString();

                /*var types = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    where assembly.GetCustomAttributes<PerperDataAttribute>().Any()
                    from type in assembly.GetTypes()
                    where type.GetCustomAttributes<PerperDataAttribute>().Any()
                    select type;*/
                var types = new Type[] {
                    typeof(Perper.WebJobs.Extensions.Cache.Notifications.CallResultNotification),
                    typeof(Perper.WebJobs.Extensions.Cache.Notifications.CallTriggerNotification),
                    typeof(Perper.WebJobs.Extensions.Cache.Notifications.StreamItemNotification),
                    typeof(Perper.WebJobs.Extensions.Cache.Notifications.StreamTriggerNotification),
                    typeof(Perper.WebJobs.Extensions.Cache.AgentData),
                    typeof(Perper.WebJobs.Extensions.Cache.CallData),
                    typeof(Perper.WebJobs.Extensions.Cache.StreamData),
                    typeof(Perper.WebJobs.Extensions.Cache.StreamDelegateType),
                    typeof(Perper.WebJobs.Extensions.Cache.StreamListener),
                };
                var serializer = ActivatorUtilities.CreateInstance<PerperBinarySerializer>(services);

                var ignite = Ignition.StartClient(new IgniteClientConfiguration
                {
                    Endpoints = new List<string> {igniteHost},
                    BinaryConfiguration = new BinaryConfiguration()
                    {
                        NameMapper = new Binary​Basic​Name​Mapper() {IsSimpleName = true},
                        Serializer = serializer,
                        TypeConfigurations = types.Select(type => new BinaryTypeConfiguration(type) {
                            Serializer = serializer,
                        }).ToList()
                    }
                });
                return ignite;
            });
            builder.Services.Configure<ServiceProviderOptions>(options => {
                options.ValidateScopes = true;
            });
            builder.AddExtension<PerperExtensionConfigProvider>();
            return builder;
        }
    }
}