using System.Diagnostics.CodeAnalysis;

#pragma warning disable 8618, 0649
namespace Perper.Protocol.Notifications
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class CallResultNotification : Notification
    {
        private readonly string call;
        private readonly string caller;

        public string Call => call;
        public string Caller => caller;

        public override string ToString()
        {
            return $"CallResultNotification({Call}, {Caller})";
        }
    }
}