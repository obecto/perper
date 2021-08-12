#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    public class StreamItemNotification : Notification
    {
        public string Stream { get; }
        public int Parameter { get; }
        public string Cache { get; }
        public long Key { get; }
        public bool Ephemeral { get; }

        public override string ToString()
        {
            return $"StreamItemNotification({Stream}, {Parameter}, {Cache}, {Key}, {Ephemeral})";
        }
    }
}