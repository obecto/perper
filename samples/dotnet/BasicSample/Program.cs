using Perper.Application;
await new PerperStartup().AddAssemblyHandlers("basic-sample").RunAsync(default).ConfigureAwait(false);