using Perper.Application;
await new PerperStartup().AddAssemblyHandlers("StreamPrinterAgent").RunAsync(default).ConfigureAwait(false);