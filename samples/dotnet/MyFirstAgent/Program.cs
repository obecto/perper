using Perper.Application;
await new PerperStartup().AddAssemblyHandlers("MyFirstAgent").WithDeployInit().RunAsync(default).ConfigureAwait(false);
