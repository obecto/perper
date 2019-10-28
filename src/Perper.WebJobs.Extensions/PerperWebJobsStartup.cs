using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Perper.WebJobs.Extensions;

[assembly: WebJobsStartup(typeof(PerperWebJobsStartup))]

namespace Perper.WebJobs.Extensions
{
    public class PerperWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddPerper();
        }
    }
}