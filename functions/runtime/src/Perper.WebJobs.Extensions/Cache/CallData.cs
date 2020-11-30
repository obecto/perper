using Perper.WebJobs.Extensions.Config;

namespace Perper.WebJobs.Extensions.Cache
{
    #pragma warning disable 8618
    [PerperData]
    public class CallData : IInstanceData
    {
        public string Agent { get; set; }
        public string AgentDelegate { get; set; }
        public string Delegate { get; set; }
        public string CallerAgentDelegate { get; set; }
        public string Caller { get; set; }
        public bool Finished { get; set; }
        public bool LocalToData { get; set; }
        public object?[]? Parameters { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
    }
}