using System;
using System.Collections.Generic;
using Apache.Ignite.Core.Binary;

namespace Perper.Protocol.Cache
{
    public class StreamData
    {
        public string Name { get; set; }
        public string Delegate { get; set; }
        public StreamDelegateType DelegateType { get; set; }

        public IBinaryObject Params { get; set; }
        public Dictionary<string, string[]> StreamParams { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public string? IndexType { get; set; }
        public IEnumerable<KeyValuePair<string, string>>? IndexFields { get; set; }

        public Dictionary<string, WorkerData> Workers { get; set; }

        public StreamData(string name, string delegateName, StreamDelegateType delegateType, IBinaryObject dataParams, Dictionary<string, string[]> streamParams, string? indexType = null, IEnumerable<KeyValuePair<string, string>>? indexFields = null)
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
    }
}