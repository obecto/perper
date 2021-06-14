using System.Collections;

namespace Perper.Protocol.Cache.Standard
{
    public class PerperStream
    {
        // NOTE: While ignite is case-insensitive with fields, it still duplicates the schema entries, hence the whole public/private dance; unfortunatelly it does mean duplicating fields four times
        private string stream;
        private Hashtable? filter;
        private bool replay;
        private bool localToData;

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