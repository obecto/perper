namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    public class CallResultNotification : Notification
    {
        public string Call { get; set; }
        public string Caller { get; set; }
    }
}