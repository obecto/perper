using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace Perper.Application
{
    [Obsolete("Use `Host.CreateDefaultBuilder().ConfigurePerper(...).Build().RunAsync()` instead")]
    public class PerperStartup : PerperBuilder
    {
        public IHostBuilder HostBuilder { get; }

        private PerperStartup(IHostBuilder hostBuilder) : base(hostBuilder) => HostBuilder = hostBuilder;

        public PerperStartup() : this(Host.CreateDefaultBuilder())
        {
        }

        [Obsolete("Use `Host.CreateDefaultBuilder().ConfigurePerper(perper => perper.AddAssemblyHandlers(...)).RunAsync()` instead")]
        public static Task RunAsync(string agent, CancellationToken cancellationToken = default)
        {
            return new PerperStartup().AddAssemblyHandlers(agent).RunAsync(cancellationToken);
        }
    }
}