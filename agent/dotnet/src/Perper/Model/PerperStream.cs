using System.Diagnostics.CodeAnalysis;

using Apache.Ignite.Core.Binary;

namespace Perper.Model
{
    // Also defined through protobufs
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "All that is *Stream is not made of Byte-s")]
    public partial class PerperStream : IBinarizable
    {
        public const long StartKeyLive = -1;

        public PerperStream(string stream, long startIndex = StartKeyLive, long stride = 0, bool localToData = false) : this()
        {
            Stream = stream;
            StartKey = startIndex;
            Stride = stride;
            LocalToData = localToData;
        }

        public bool LocalToData { get; set; }
        public bool IsReplayed => StartKey != StartKeyLive;
        public bool IsPacked => Stride != 0;

        void IBinarizable.ReadBinary(IBinaryReader reader)
        {
            Stream = reader.ReadString("stream");
            StartKey = reader.ReadLong("startKey");
            Stride = reader.ReadLong("stride");
        }

        void IBinarizable.WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("stream", Stream);
            writer.WriteLong("startKey", StartKey);
            writer.WriteLong("stride", Stride);
        }
    }
}