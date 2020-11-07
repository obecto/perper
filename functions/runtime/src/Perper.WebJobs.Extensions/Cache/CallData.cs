using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Cache
{
    public class CallData
    {
        public string Agent { get; set; }
        public string AgentDelegate { get; set; }
        public string Delegate { get; set; }
        public string CallerAgentDelegate { get; set; }
        public string Caller { get; set; }
        public bool Finished { get; set; }
        public bool LocalToData { get; set; }
    }
}