using System.Collections.Generic;
using Apache.Ignite.Core.Binary;

namespace Perper.Protocol.Cache
{
    public class WorkerData : IBinarizable
    {
        public string Name { get; set; }
        public string Delegate { get; set; }

        public string Caller { get; set; }

        public Dictionary<string, object?> Params { get; set; }

        public bool Finished { get; set; }

        public WorkerData(string name, string delegateName, string caller, Dictionary<string, object?> dataParams, bool finished = false)
        {
            Name = name;
            Delegate = delegateName;
            Caller = caller;
            Params = dataParams;
            Finished = false;
        }

        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("caller", Caller);
            writer.WriteString("delegate", Delegate);
            writer.WriteBoolean("finished", Finished);
            writer.WriteString("name", Name);
            writer.WriteDictionary("params", Params);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            Caller = reader.ReadString("caller");
            Delegate = reader.ReadString("delegate");
            Finished = reader.ReadBoolean("finished");
            Name = reader.ReadString("name");
            Params = ((Dictionary<string, object?>)reader.ReadDictionary("params", s => new Dictionary<string, object?>(s)));
        }
    }
}