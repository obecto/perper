using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Perper.WebJobs.Extensions.Cache.Notifications;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class FabricService
    {
        public async IAsyncEnumerable<Notification> GetNotifications(string delegateName,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return null;
        }
    }
}