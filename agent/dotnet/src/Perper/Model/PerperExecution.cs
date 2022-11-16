using Apache.Ignite.Core.Binary;

namespace Perper.Model
{
    // Also defined through protobufs
    public partial class PerperExecution : IBinarizable
    {
        public PerperExecution(string execution) : this() => Execution = execution;

        void IBinarizable.ReadBinary(IBinaryReader reader)
        {
            Execution = reader.ReadString("execution");
        }

        void IBinarizable.WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("execution", Execution);
        }
    }
}