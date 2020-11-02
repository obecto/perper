using System;
using System.Collections.Generic;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Protocol.Cache
{
    public class StreamData : IBinarizable
    {
        public string Name { get; set; }
        public string Delegate { get; set; }
        public StreamDelegateType DelegateType { get; set; }

        public Dictionary<string, object?> Params { get; set; }
        public Dictionary<string, StreamParam[]> StreamParams { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public string? IndexType { get; set; }
        public Dictionary<string, string>? IndexFields { get; set; }

        public Dictionary<string, WorkerData> Workers { get; set; }

        public StreamData() : this("", "", StreamDelegateType.Function, null!, new Dictionary<string, StreamParam[]>(), null, null)
        {
        }

        public StreamData(string name, string delegateName, StreamDelegateType delegateType, Dictionary<string, object?> dataParams, Dictionary<string, StreamParam[]> streamParams, string? indexType = null, Dictionary<string, string>? indexFields = null)
        {
            Name = name;
            Delegate = delegateName;
            DelegateType = delegateType;
            Params = dataParams;
            StreamParams = streamParams;
            IndexType = indexType;
            IndexFields = indexFields;

            Workers = new Dictionary<string, WorkerData>();
        }

        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteString("delegate", Delegate);
            writer.WriteEnum("delegateType", DelegateType);
            writer.WriteDictionary("indexFields", IndexFields);
            writer.WriteString("indexType", IndexType);
            writer.WriteTimestamp("lastModified", LastModified);
            writer.WriteString("name", Name);
            writer.WriteDictionary("params", Params);
            writer.WriteDictionary("streamParams", StreamParams);
            writer.WriteDictionary("workers", Workers);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            Delegate = reader.ReadString("delegate");
            DelegateType = reader.ReadEnum<StreamDelegateType>("delegateType");
            IndexFields = (Dictionary<string, string>)reader.ReadDictionary("indexFields", s => new Dictionary<string, string>(s));
            IndexType = reader.ReadString("indexType");
            LastModified = reader.ReadTimestamp("lastModified")!.Value;
            Name = reader.ReadString("name");
            Params = ((Dictionary<string, object?>)reader.ReadDictionary("params", s => new Dictionary<string, object?>(s)));
            StreamParams = ((Dictionary<string, StreamParam[]>)reader.ReadDictionary("streamParams", s => new Dictionary<string, StreamParam[]>(s)));
            Workers = (Dictionary<string, WorkerData>)reader.ReadDictionary("workers", s => new Dictionary<string, WorkerData>(s));
        }
    }
}