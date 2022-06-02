using Microsoft.Extensions.Hosting;

using Perper.Application;
Host.CreateDefaultBuilder().ConfigurePerper(perper => perper.AddAssemblyHandlers("basic-sample")).Build().Run();