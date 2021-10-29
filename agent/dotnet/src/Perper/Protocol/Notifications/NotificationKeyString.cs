using System.Diagnostics.CodeAnalysis;

using Apache.Ignite.Core.Cache.Affinity;

namespace Perper.Protocol.Notifications
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class NotificationKeyString : NotificationKey
    {
        // NOTE: While ignite is case-insensitive with fields in general, AffinityKeyMapped isn't
        // NOTE: Order of the fields is important!
        [AffinityKeyMapped]
        private readonly string affinity;
        private readonly long key;

        public NotificationKeyString(string affinity, long key)
        {
            this.affinity = affinity;
            this.key = key;
        }

        public string Affinity => affinity;
        public long Key => key;

        public override string ToString()
        {
            return $"NotificationKeyString({Key} [{Affinity}])";
        }
    }
}