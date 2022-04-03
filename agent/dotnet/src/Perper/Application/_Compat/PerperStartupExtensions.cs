using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Perper.Application
{
    public static class PerperStartupExtensions
    {
        [Obsolete("Use `Host.CreateDefaultBuilder().ConfigurePerper(...).Build().RunAsync()` instead")]
        public static Task RunAsync(this IPerperBuilder builder, CancellationToken cancellationToken = default) => ((PerperStartup)builder).HostBuilder.Build().RunAsync(cancellationToken);
    }
}