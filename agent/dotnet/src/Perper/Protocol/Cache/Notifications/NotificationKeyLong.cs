using System.Diagnostics.CodeAnalysis;

using Apache.Ignite.Core.Cache.Affinity;

namespace Perper.Protocol.Cache.Notifications
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class NotificationKeyLong : NotificationKey
    {
        // NOTE: While ignite is case-insensitive with fields in general, AffinityKeyMapped isn't
        // NOTE: Order of the fields is important!
        [AffinityKeyMapped]
        private readonly long affinity;
        private readonly long key;

        public NotificationKeyLong(long affinity, long key)
        {
            this.affinity = affinity;
            this.key = key;
        }

        public long Affinity => affinity;
        public long Key => key;

        public override string ToString()
        {
            return $"NotificationKeyLong({Key}, {Affinity})";
        }
    }
}