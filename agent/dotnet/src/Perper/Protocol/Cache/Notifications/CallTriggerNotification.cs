using System.Diagnostics.CodeAnalysis;

#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache.Notifications
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class CallTriggerNotification : Notification
    {
        private readonly string call;
        private readonly string instance;
        private readonly string @delegate;

        public string Call => call;
        public string Instance => instance;
        public string Delegate => @delegate;

        public override string ToString()
        {
            return $"CallTriggerNotification({Call}, {Instance}, {Delegate})";
        }
    }
}