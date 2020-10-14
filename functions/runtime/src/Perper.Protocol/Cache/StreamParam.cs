using System.Collections.Generic;
using Apache.Ignite.Core.Binary;

namespace Perper.Protocol.Cache
{
    public class StreamParam : IBinarizable
    {
        public string Stream { get; set; }
        public Dictionary<string, object?> Filter { get; set; }

        public StreamParam() : this("", new Dictionary<string, object?>())
        {
        }

        public StreamParam(string stream, Dictionary<string, object?> filter)
        {
            Stream = stream;
            Filter = filter;
        }

        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("stream", Stream);
            writer.WriteDictionary("filter", Filter);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            Stream = reader.ReadString("stream");
            Filter = (Dictionary<string, object?>)reader.ReadDictionary("filter", s => new Dictionary<string, object?>(s));
        }
    }
}