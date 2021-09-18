#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    public class CallTriggerNotification : Notification
    {
        public string Call { get; }
        public string Instance { get; }
        public string Delegate { get; }

        public override string ToString()
        {
            return $"CallTriggerNotification({Call}, {Instance}, {Delegate})";
        }
    }
}