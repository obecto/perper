namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    public class StreamTriggerNotification : Notification
    {
        public string Stream { get; set; }
        public string Delegate { get; set; }
    }
}