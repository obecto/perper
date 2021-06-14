#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    public class CallTriggerNotification : Notification
    {
        // NOTE: While ignite is case-insensitive with fields, it still duplicates the schema entries, hence the whole public/private dance
        private string call;
        private string @delegate;

        public string Call => call;
        public string Delegate => @delegate;

        public override string ToString()
        {
            return $"CallTriggerNotification({Call}, {Delegate})";
        }
    }
}