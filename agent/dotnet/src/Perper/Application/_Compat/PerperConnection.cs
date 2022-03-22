using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Perper.Protocol;

namespace Perper.Application
{
    [Obsolete("Use `Host.CreateDefaultBuilder().ConfigurePerper(...).Build()` instead")]
    public static class PerperConnection
    {
        private static readonly IHost ConfiguredHost = Host.CreateDefaultBuilder().ConfigurePerper(_ => { }).Build();

        public static Task<FabricService> EstablishConnection()
        {
            return Task.FromResult(ConfiguredHost.Services.GetRequiredService<FabricService>());
        }

        public static (string, string) ConfigureInstance()
        {
            var perperConfiguration = ConfiguredHost.Services.GetRequiredService<IOptions<PerperConfiguration>>().Value;
            return (perperConfiguration.Agent ?? "", perperConfiguration.Instance ?? "");
        }
    }
}