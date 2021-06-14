#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    public class CallResultNotification : Notification
    {
        // NOTE: While ignite is case-insensitive with fields, it still duplicates the schema entries, hence the whole public/private dance
        private string call;
        private string caller;

        public string Call => call;
        public string Caller => caller;

        public override string ToString()
        {
            return $"CallResultNotification({Call}, {Caller})";
        }
    }
}