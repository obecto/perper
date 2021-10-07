using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Perper.Protocol.Instance
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class StreamListener
    {
        private readonly string callerAgent;
        private readonly string callerInstance;
        private readonly string caller;
        private readonly int parameter;

        private readonly bool replay;
        private readonly bool localToData;
        private readonly Hashtable? filter;

        public StreamListener(
            string callerAgent,
            string callerInstance,
            string caller,
            int parameter,
            bool replay,
            bool localToData,
            Hashtable? filter = null)
        {
            this.callerAgent = callerAgent;
            this.callerInstance = callerInstance;
            this.caller = caller;
            this.parameter = parameter;
            this.replay = replay;
            this.localToData = localToData;
            this.filter = filter;
        }

        public string CallerAgent => callerAgent;
        public string CallerInstance => callerInstance;
        public string Caller => caller;
        public int Parameter => parameter;

        public bool Replay => replay;
        public bool LocalToData => localToData;
        public Hashtable? Filter => filter;
    }
}