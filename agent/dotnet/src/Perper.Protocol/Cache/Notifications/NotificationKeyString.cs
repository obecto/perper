using Apache.Ignite.Core.Cache.Affinity;

namespace Perper.Protocol.Cache.Notifications
{
    public class NotificationKeyString : NotificationKey
    {
        // NOTE: While ignite is case-insensitive with fields in general, AffinityKeyMapped isn't
        // NOTE: Order of the fields is important!
        [AffinityKeyMapped]
        private readonly string affinity;

        public NotificationKeyString(string affinity, long key)
        {
            this.affinity = affinity;
            Key = key;
        }

        public string Affinity => affinity;
        public long Key { get; }

        public override string ToString()
        {
            return $"NotificationKeyString({Key} [{Affinity}])";
        }
    }
}