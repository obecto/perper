using Apache.Ignite.Core.Cache.Affinity;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    [PerperData]
    public class NotificationKeyString : NotificationKey
    {
        public NotificationKeyString(long _key, string _affinity)
        {
            Key = _key;
            Affinity = _affinity;
        }

        public long Key { get; set; }

        [AffinityKeyMapped]
        public string Affinity { get; set; }

        public override string ToString()
        {
            return $"({Key} [{Affinity}])";
        }
    }
}