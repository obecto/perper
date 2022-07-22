using Microsoft.Extensions.Hosting;

using Perper.Application;
Host.CreateDefaultBuilder().ConfigurePerper(perper => perper.AddAssemblyHandlers("container-usage-sample")).Build().Run();