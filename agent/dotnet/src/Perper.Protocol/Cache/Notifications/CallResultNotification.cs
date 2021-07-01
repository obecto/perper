#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    public class CallResultNotification : Notification
    {
        public string Call { get; }
        public string Caller { get; }

        public override string ToString()
        {
            return $"CallResultNotification({Call}, {Caller})";
        }
    }
}