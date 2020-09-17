using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Messaging.FunctionApp
{
    public class Peering
    {
        [FunctionName(nameof(Peering))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("streams")] IPerperStream[] streams,
            CancellationToken cancellationToken)
        {
            await context.BindOutput(streams, cancellationToken);
        }
    }
}