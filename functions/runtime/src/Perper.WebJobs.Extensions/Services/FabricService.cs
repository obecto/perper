using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Perper.WebJobs.Extensions.Protobuf;
using Notification = Perper.WebJobs.Extensions.Cache.Notifications.Notification;

namespace Perper.WebJobs.Extensions.Services
{
    public class FabricService
    {
        public async IAsyncEnumerable<Notification> GetNotifications(string delegateName,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Hosted: use two separate streams for every delegate - one for runtime and one for model
            // Workers: separate GRPC connections
            yield return null;
        }

        public async IAsyncEnumerable<Notification> GetStreamItemNotification(string delegateName, string stream, string parameter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return null;
        }
    }
}