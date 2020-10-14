using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
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
            builder.Services.AddSingleton<IPerperFabricContext, PerperFabricContext>();
            builder.Services.AddOptions<PerperFabricConfig>()
                .Configure<IConfiguration>((coinApiConfigurationSettings, configuration) =>
                {
                    configuration
                        .GetSection("Perper")
                        .Bind(coinApiConfigurationSettings);
                });
            return builder;
        }
    }
}