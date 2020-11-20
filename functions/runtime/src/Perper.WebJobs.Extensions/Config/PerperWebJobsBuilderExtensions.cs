using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Config
{
    public static class PerperWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddPerper(this IWebJobsBuilder builder)
        {
            builder.Services.AddScoped(typeof(IContext), typeof(Context));
            builder.Services.AddScoped(typeof(IState), typeof(State));
            builder.Services.AddScoped(typeof(IStateEntry<>), typeof(StateEntry<>));
            builder.AddExtension<PerperExtensionConfigProvider>();
            return builder;
        }
    }
}