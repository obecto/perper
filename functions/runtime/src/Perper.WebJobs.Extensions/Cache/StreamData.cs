using System;
using System.Collections.Generic;

namespace Perper.WebJobs.Extensions.Cache
{
    public class StreamData : IInstanceData
    {
        public string Agent { get; set; }
        public string AgentDelegate { get; set; }
        public string Delegate { get; set; }
        public StreamDelegateType DelegateType { get; set; }
        public object?[]? Parameters { get; set; }
        public List<StreamListener> Listeners { get; set; }
        public string? IndexType { get; set; }
        public Dictionary<string, string>? IndexFields { get; set; }
        public bool Ephemeral { get; set; }
        public DateTime LastModified { get; set; }
    }
}