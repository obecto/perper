using System.Collections.Generic;
using Apache.Ignite.Core;

namespace Perper.Fabric.Streams
{
    //TODO: Consider changing it to Ignite Service and use it as StreamsLocator
    public class StreamsCache
    {
        private readonly IIgnite _ignite;

        private readonly Dictionary<string, Stream> _streams;

        public StreamsCache(IIgnite ignite)
        {
            _ignite = ignite;

            _streams = new Dictionary<string, Stream>();
        }

        public Stream GetStream(string name)
        {
            if (_streams.TryGetValue(name, out var stream)) return stream;

            stream = new Stream(name, _ignite, this);
            _streams[name] = stream;
            return stream;
        }
    }
}