
using Apache.Ignite.Core.Binary;

namespace Perper.Model
{
    // Also defined through protobufs
    public partial class PerperInstance : IBinarizable
    {
        public PerperInstance(string agent, string instance) : this()
        {
            Agent = agent;
            Instance = instance;
        }

        void IBinarizable.ReadBinary(IBinaryReader reader)
        {
            Agent = reader.ReadString("agent");
            Instance = reader.ReadString("instance");
        }

        void IBinarizable.WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("agent", Agent);
            writer.WriteString("instance", Instance);
        }
    }
}