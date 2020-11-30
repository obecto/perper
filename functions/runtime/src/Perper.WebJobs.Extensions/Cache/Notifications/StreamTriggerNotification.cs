namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    #pragma warning disable 8618
    public class StreamTriggerNotification : Notification
    {
        public string Stream { get; set; }
        public string Delegate { get; set; }
    }
}