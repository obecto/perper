using System.Collections.Generic;

namespace Perper.WebJobs.Extensions.Cache
{
    public class StreamListener
    {
        public string AgentDelegate { get; set; }
        public string Stream { get; set; }
        public string Parameter { get; set; }
        public Dictionary<string, object> Filter { get; set; }
        public bool LocalToData { get; set; }
    }
}