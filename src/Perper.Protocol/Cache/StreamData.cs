using Apache.Ignite.Core.Binary;

namespace Perper.Protocol.Cache
{
    public class StreamData
    {
        public string Name { get; set; }
        public string Delegate { get; set; }
        public StreamDelegateType DelegateType { get; set; }

        public IBinaryObject Params { get; set; }

        public StreamRef GetRef()
        {
            return new StreamRef
            {
                StreamName = Name
            };
        }
    }
}