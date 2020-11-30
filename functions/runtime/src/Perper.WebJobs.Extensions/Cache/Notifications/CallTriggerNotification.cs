using Perper.WebJobs.Extensions.Config;

#pragma warning disable 8618
namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    [PerperData]
    public class CallTriggerNotification : Notification
    {
        public string Call { get; set; }
        public string Delegate { get; set; }
    }
}