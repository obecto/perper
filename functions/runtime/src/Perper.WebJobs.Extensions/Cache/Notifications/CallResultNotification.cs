namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    #pragma warning disable 8618
    public class CallResultNotification : Notification
    {
        public string Call { get; set; }
        public string Caller { get; set; }
    }
}