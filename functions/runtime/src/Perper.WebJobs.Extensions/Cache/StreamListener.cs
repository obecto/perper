using System.Collections.Generic;
using Perper.WebJobs.Extensions.Model;

#pragma warning disable 8618
namespace Perper.WebJobs.Extensions.Cache
{
    [PerperData]
    public class StreamListener
    {
        public string AgentDelegate { get; set; }
        public string Stream { get; set; }
        public int Parameter { get; set; }
        public Dictionary<string, object?> Filter { get; set; }
        public bool Replay { get; set; }
        public bool LocalToData { get; set; }
    }
}