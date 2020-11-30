using Perper.WebJobs.Extensions.Config;

namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    #pragma warning disable 8618
    [PerperData]
    public class StreamItemNotification : Notification
    {
        public string Stream { get; set; }
        public int Parameter { get; set; }
        public string Cache { get; set; }
        public long Key { get; set; }
        public bool Ephemeral { get; set; }
    }
}