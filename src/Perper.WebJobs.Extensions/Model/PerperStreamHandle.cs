using Perper.Protocol.Header;

namespace Perper.WebJobs.Extensions.Model
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