using System.Collections.Generic;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions
{
    public static class PerperWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddPerper(this IWebJobsBuilder builder)
        {
            builder.AddExtension<PerperExtensionConfigProvider>();
            builder.Services.AddSingleton(provider =>
                new PerperFabricContext(Ignition.StartClient(new IgniteClientConfiguration
                {
                    Endpoints = new List<string> {"127.0.0.1"}
                }).GetBinary()));
            return builder;
        }
    }
}