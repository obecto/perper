#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    public class StreamTriggerNotification : Notification
    {
        public string Stream { get; }
        public string Instance { get; }
        public string Delegate { get; }

        public override string ToString()
        {
            return $"StreamTriggerNotification({Stream}, {Instance}, {Delegate})";
        }
    }
}