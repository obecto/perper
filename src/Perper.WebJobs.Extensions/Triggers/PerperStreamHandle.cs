using Perper.Protocol.Header;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamHandle : IPerperStreamHandle
    {
        public StreamHeader Header { get; set; }
        
        public PerperStreamHandle(StreamHeader header)
        {
            Header = header;
        }
    }
}