using Perper.WebJobs.Extensions.Cache;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerValue
    {
        public bool IsCall { get; }
        public string InstanceName { get; }

        public PerperTriggerValue(bool isCall, string instanceName)
        {
            IsCall = isCall;
            InstanceName = instanceName;
        }
    }
}