using Perper.Application;
await new PerperStartup().AddAssemblyHandlers("container-usage-sample").RunAsync(default).ConfigureAwait(false);