#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    public class StreamTriggerNotification : Notification
    {
        // NOTE: While ignite is case-insensitive with fields, it still duplicates the schema entries, hence the whole public/private dance
        private string stream;
        private string @delegate;

        public string Stream => stream;
        public string Delegate => @delegate;

        public override string ToString()
        {
            return $"StreamTriggerNotification({Stream}, {Delegate})";
        }
    }
}