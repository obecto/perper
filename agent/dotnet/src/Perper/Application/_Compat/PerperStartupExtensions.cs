using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Perper.Application
{
    public static class PerperStartupExtensions
    {
        [Obsolete("Use `Host.CreateDefaultBuilder().ConfigurePerper(...).Build().RunAsync()` instead")]
        public static Task RunAsync(this IPerperBuilder builder, CancellationToken cancellationToken = default) =>
            ((PerperStartup)builder).HostBuilder.Build().RunAsync(cancellationToken);

        [Obsolete("Use `AddDeploySingletonHandler()` instead -- Note that exact semantics have changed.")]
        public static IPerperBuilder WithDeployInit(this IPerperBuilder builder) =>
            builder.AddDeploySingletonHandler(PerperConnection.ConfigureInstance().Item1);

        public static IPerperBuilder AddInitHandler(this IPerperBuilder builder, string agent, Delegate handler) =>
            builder.AddHandler(agent, "Init", handler);

        public static IPerperBuilder AddInitHandler<T>(this IPerperBuilder builder, string agent) =>
            builder.AddHandler(agent, "Init", typeof(T));

        public static IPerperBuilder AddInitHandler(this IPerperBuilder builder, string agent, Type initType) =>
            builder.AddHandler(agent, "Init", initType);

        public static IPerperBuilder AddInitHandler(this IPerperBuilder builder, string agent, Type initType, MethodInfo method) =>
            builder.AddHandler(agent, "Init", initType, method);
    }
}