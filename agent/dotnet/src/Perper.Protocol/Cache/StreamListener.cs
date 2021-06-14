using System.Collections;

#pragma warning disable 8618, 0649
namespace Perper.Protocol.Cache
{
    public class StreamListener
    {
        // NOTE: While ignite is case-insensitive with fields, it still duplicates the schema entries, hence the whole public/private dance; unfortunatelly it does mean duplicating fields four times
        private string callerAgent;
        private string caller;
        private int parameter;
        private Hashtable? filter;
        private bool replay;
        private bool localToData;

        public StreamListener(
            string callerAgent,
            string caller,
            int parameter,
            bool replay,
            bool localToData,
            Hashtable? filter = null)
        {
            this.callerAgent = callerAgent;
            this.caller = caller;
            this.parameter = parameter;
            this.filter = filter;
            this.replay = replay;
            this.localToData = localToData;
        }

        public string CallerAgent => callerAgent;
        public string Caller => caller;
        public int Parameter => parameter;
        public Hashtable? Filter => filter;
        public bool Replay => replay;
        public bool LocalToData => localToData;
    }
}
