using Apache.Ignite.Core.Binary;

namespace Perper.Protocol.Cache
{
    public class WorkerData : IBinarizable
    {
        public string Name { get; set; }
        public string Delegate { get; set; }

        public string Caller { get; set; }

        public IBinaryObject Params { get; set; }

        public WorkerData(string name, string delegateName, string caller, IBinaryObject dataParams)
        {
            Name = name;
            Delegate = delegateName;
            Caller = caller;
            Params = dataParams;
        }

        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("caller", Caller);
            writer.WriteString("delegate", Delegate);
            writer.WriteString("name", Name);
            writer.WriteObject("params", Params);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            Caller = reader.ReadString("caller");
            Delegate = reader.ReadString("delegate");
            Name = reader.ReadString("name");
            Params = reader.ReadObject<IBinaryObject>("params");
        }
    }
}