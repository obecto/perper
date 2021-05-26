using System;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

using Perper.WebJobs.Extensions;
using Perper.WebJobs.Extensions.Config;

[assembly: WebJobsStartup(typeof(PerperWebJobsStartup))]

namespace Perper.WebJobs.Extensions
{
    public class PerperWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            builder.AddPerper();
        }
    }
}