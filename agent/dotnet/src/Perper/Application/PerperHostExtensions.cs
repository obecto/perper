using System;

using Microsoft.Extensions.Hosting;

namespace Perper.Application
{
    public static class PerperHostExtensions
    {
        public static IHostBuilder ConfigurePerper(this IHostBuilder builder, Action<IPerperBuilder> configure)
        {
            var perperBuilder = new PerperBuilder(builder);
            configure(perperBuilder);
            return builder;
        }
    }
}