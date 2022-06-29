using Microsoft.Extensions.Hosting;

using Perper.Application;
Host.CreateDefaultBuilder().ConfigurePerper(perper => perper.AddDeploySingletonHandler("basic-sample").AddAssemblyHandlers("basic-sample")).Build().Run();