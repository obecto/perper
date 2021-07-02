using Apache.Ignite.Core.Cache.Affinity;

namespace Perper.Protocol.Cache.Notifications
{
    public class NotificationKeyLong : NotificationKey
    {
        // NOTE: While ignite is case-insensitive with fields in general, AffinityKeyMapped isn't
        // NOTE: Order of the fields is important!
        [AffinityKeyMapped]
        private readonly long affinity;

        public NotificationKeyLong(long affinity, long key)
        {
            this.affinity = affinity;
            Key = key;
        }

        public long Affinity => affinity;
        public long Key { get; }

        public override string ToString()
        {
            return $"NotificationKeyLong({Key}, {Affinity})";
        }
    }
}