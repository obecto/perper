using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricInput
    {
        public async Task Listen(Action<IBinaryObject> listener, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
        }
    }
}