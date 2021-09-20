using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Perper.Protocol.Cache.Standard
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class PerperStream
    {
        private readonly string stream;
        private readonly Hashtable? filter;
        private readonly bool replay;
        private readonly bool localToData;

        public PerperStream(
            string stream,
            Hashtable? filter = null,
            bool replay = false,
            bool localToData = false)
        {
            this.stream = stream;
            this.filter = filter;
            this.replay = replay;
            this.localToData = localToData;
        }

        public string Stream => stream;
        public Hashtable? Filter => filter;
        public bool Replay => replay;
        public bool LocalToData => localToData;
    }
}