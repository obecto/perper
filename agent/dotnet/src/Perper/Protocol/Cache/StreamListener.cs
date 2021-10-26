using System.Diagnostics.CodeAnalysis;

namespace Perper.Protocol.Cache
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class StreamListener
    {
        private readonly string stream;
        private readonly long position;

        public StreamListener(
            string stream,
            long position)
        {
            this.stream = stream;
            this.position = position;
        }

        public string Stream => stream;
        public long Position => position;
    }
}