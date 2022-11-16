using Apache.Ignite.Core.Binary;

namespace Perper.Model
{
    // Also defined through protobufs
    public partial class PerperList : IBinarizable
    {
        public PerperList(string list) : this() => List = list;

        void IBinarizable.ReadBinary(IBinaryReader reader)
        {
            List = reader.ReadString("name"); // TODO
        }

        void IBinarizable.WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("name", List);
        }
    }
}