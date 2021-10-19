using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Perper.Model
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "All that is *Stream is not made of Byte-s")]
    public class PerperStream
    {
        private readonly string stream;
        private readonly Hashtable? filter;
        private readonly long startIndex;
        //private readonly bool localToData;

        public PerperStream(
            string stream,
            Hashtable? filter = null,
            long startIndex = -1)
        {
            this.stream = stream;
            this.filter = filter;
            this.startIndex = startIndex;
            //this.localToData = localToData;
        }

        public string Stream => stream;
        public Hashtable? Filter => filter;
        public long StartIndex => startIndex;

        public bool Replay => startIndex == -1;
    }
}