using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;

namespace Perper.WebJobs.Extensions
{
    public static class PerperWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddPerper(this IWebJobsBuilder builder)
        {
            builder.AddExtension<PerperExtensionConfigProvider>();
            return builder;
        }
    }
}