using Apache.Ignite.Core.Binary;

namespace Perper.Protocol.Cache
{
    public class WorkerData
    {
        public string Name { get; set; }
        public string Delegate { get; set; }

        public string Caller { get; set; }

        public IBinaryObject Params { get; set; }
    }
}