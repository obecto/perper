namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    public class CallTriggerNotification : Notification
    {
        public string Call { get; set; }
        public string Delegate { get; set; }
    }
}