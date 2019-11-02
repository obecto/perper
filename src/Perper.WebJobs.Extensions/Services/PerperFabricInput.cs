using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricInput
    {
        public async Task Listen(Func<IBinaryObject, Task> listener, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
        }
    }
}