using Perper.Application;
await new PerperStartup().AddAssemblyHandlers("container-usage-sample").WithDeployInit().RunAsync(default).ConfigureAwait(false);