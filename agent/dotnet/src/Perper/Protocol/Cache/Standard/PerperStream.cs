using System.Collections;

namespace Perper.Protocol.Cache.Standard
{
    public class PerperStream
    {
        public PerperStream(
            string stream,
            Hashtable? filter = null,
            bool replay = false,
            bool localToData = false)
        {
            Stream = stream;
            Filter = filter;
            Replay = replay;
            LocalToData = localToData;
        }

        public string Stream { get; }
        public Hashtable? Filter { get; }
        public bool Replay { get; }
        public bool LocalToData { get; }
    }
}