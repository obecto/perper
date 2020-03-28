using System.Security.Principal;
using System.Text.Json.Serialization;

namespace Perper.Protocol.Notifications
{
    public class Notification
    {
        public NotificationType Type { get; set; }
        
        public string Stream { get; set; }
        public string Worker { get; set; }
        public string Delegate { get; set; }

        public string Parameter { get; set; }
        public string ParameterStream { get; set; }
        public long ParameterStreamItemKey { get; set; }

        [JsonIgnore]
        public object ParameterStreamItem { get; set; }
    }
}