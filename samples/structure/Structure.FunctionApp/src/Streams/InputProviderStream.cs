using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Structure.Streams
{
    public class InputProviderStream
    {
        [FunctionName(nameof(InputProviderStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("input")] IPerperStream input,
            [Perper("output")] IAsyncCollector<IPerperStream> output,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            await output.AddAsync(input, cancellationToken);
        }
    }
}