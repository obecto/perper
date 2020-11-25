namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    public class StreamItemNotification : Notification
    {
        public string Stream { get; set; }
        public int Parameter { get; set; }
        public string Cache { get; set; }
        public long Key { get; set; }
        public bool Ephemeral { get; set; }
    }
}