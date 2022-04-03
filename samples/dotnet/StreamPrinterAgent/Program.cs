using Microsoft.Extensions.Hosting;

using Perper.Application;
#pragma warning disable CA1812
Host.CreateDefaultBuilder().ConfigurePerper(perper => perper.AddAssemblyHandlers("StreamPrinterAgent")).Build().Run();
#pragma warning restore CA1812