namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    #pragma warning disable 8618
    public class CallTriggerNotification : Notification
    {
        public string Call { get; set; }
        public string Delegate { get; set; }
    }
}