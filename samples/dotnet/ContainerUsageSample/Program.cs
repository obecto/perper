using Perper.Application;
#pragma warning disable CA1812
await new PerperStartup().AddAssemblyHandlers("container-usage-sample").RunAsync(default).ConfigureAwait(false);
#pragma warning restore CA1812