using Apache.Ignite.Core.Binary;

namespace Perper.Protocol.Cache
{
    public class StreamParam : IBinarizable
    {
        public string Stream { get; set; }
        public IBinaryObject? Filter { get; set; }

        public StreamParam() : this("", null)
        {
        }

        public StreamParam(string stream, IBinaryObject? filter)
        {
            Stream = stream;
            Filter = filter;
        }

        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("stream", Stream);
            writer.WriteObject("filter", Filter);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            Stream = reader.ReadString("stream");
            Filter = reader.ReadObject<IBinaryObject>("filter");
        }
    }
}