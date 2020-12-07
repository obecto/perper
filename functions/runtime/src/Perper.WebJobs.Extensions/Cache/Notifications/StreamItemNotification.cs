using Perper.WebJobs.Extensions.Model;

#pragma warning disable 8618
namespace Perper.WebJobs.Extensions.Cache.Notifications
{
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