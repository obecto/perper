using System.Diagnostics.CodeAnalysis;

using Apache.Ignite.Core.Binary;

namespace Perper.Model
{
    // Also defined through protobufs
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "We need the type name to match the Protobuf/Ignite model")]
    public partial class PerperDictionary : IBinarizable
    {
        public PerperDictionary(string dictionary) : this() => Dictionary = dictionary;

        void IBinarizable.ReadBinary(IBinaryReader reader)
        {
            Dictionary = reader.ReadString("name"); // TODO
        }

        void IBinarizable.WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("name", Dictionary);
        }
    }
}