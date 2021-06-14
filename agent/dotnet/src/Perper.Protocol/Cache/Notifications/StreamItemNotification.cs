#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    public class StreamItemNotification : Notification
    {
        // NOTE: While ignite is case-insensitive with fields, it still duplicates the schema entries, hence the whole public/private dance
        private string stream;
        private int parameter;
        private string cache;
        private long key;
        private bool ephemeral;

        public string Stream => stream;
        public int Parameter => parameter;
        public string Cache => cache;
        public long Key => key;
        public bool Ephemeral => ephemeral;

        public override string ToString()
        {
            return $"StreamItemNotification({Stream}, {Parameter}, {Cache}, {Key}, {Ephemeral})";
        }
    }
}