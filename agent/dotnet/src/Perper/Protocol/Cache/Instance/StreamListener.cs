using System.Collections;

namespace Perper.Protocol.Cache.Instance
{
    public class StreamListener
    {
        public StreamListener(
            string callerAgent,
            string caller,
            int parameter,
            bool replay,
            bool localToData,
            Hashtable? filter = null)
        {
            CallerAgent = callerAgent;
            Caller = caller;
            Parameter = parameter;
            Filter = filter;
            Replay = replay;
            LocalToData = localToData;
        }

        public string CallerAgent { get; }

        public string Caller { get; }

        public int Parameter { get; }

        public Hashtable? Filter { get; }

        public bool Replay { get; }

        public bool LocalToData { get; }
    }
}