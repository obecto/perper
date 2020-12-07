using Perper.WebJobs.Extensions.Model;

#pragma warning disable 8618
namespace Perper.WebJobs.Extensions.Cache.Notifications
{
    [PerperData]
    public class CallResultNotification : Notification
    {
        public string Call { get; set; }
        public string Caller { get; set; }
    }
}