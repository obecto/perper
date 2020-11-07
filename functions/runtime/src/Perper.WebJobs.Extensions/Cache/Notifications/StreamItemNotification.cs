namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    public class StreamItemNotification : Notification
    {
        public string Stream { get; set; }
        public string Parameter { get; set; }
        public string Cache { get; set; }
        public long Index { get; set; }
        public bool Ephemeral { get; set; }
    }
}