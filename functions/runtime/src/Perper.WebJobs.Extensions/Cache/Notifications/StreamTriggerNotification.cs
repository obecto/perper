using Perper.WebJobs.Extensions.Config;

#pragma warning disable 8618
namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    [PerperData]
    public class StreamTriggerNotification : Notification
    {
        public string Stream { get; set; }
        public string Delegate { get; set; }
    }
}