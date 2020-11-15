using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Perper.WebJobs.Extensions;
using Perper.WebJobs.Extensions.Model;

[assembly: WebJobsStartup(typeof(PerperWebJobsStartup))]

namespace Perper.WebJobs.Extensions
{
    public class PerperWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            builder.Services.AddScoped(typeof(IContext), typeof(Context));
            builder.AddPerper();
        }
    }
}