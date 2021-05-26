using Apache.Ignite.Core.Cache.Affinity;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    [PerperData]
    public class NotificationKeyLong : NotificationKey
    {
        public NotificationKeyLong(long _key, long _affinity)
        {
            Key = _key;
            Affinity = _affinity;
        }

        public long Key { get; set; }

        [AffinityKeyMapped]
        public long Affinity { get; set; }

        public override string ToString()
        {
            return $"({Key} [{Affinity}])";
        }
    }
}