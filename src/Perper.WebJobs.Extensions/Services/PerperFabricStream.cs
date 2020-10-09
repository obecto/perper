using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using System.Collections.Generic;
using Perper.WebJobs.Extensions.Model;
using Perper.Protocol.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IPerperStream, IBinarizable
    {
        public string StreamName { get; private set; }

        public bool Subscribed { get; private set; }

        public Dictionary<string, object?> Filter { get; private set; }

        public string DeclaredDelegate { get; }

        public Type? DeclaredType { get; }

        private Func<Task>? _dispose;

        public PerperFabricStream(string streamName, bool subscribed = false, Dictionary<string, object?>? filter = null, string declaredDelegate = "", Type? declaredType = null, Func<Task>? dispose = null)
        {
            StreamName = streamName;
            Subscribed = subscribed;
            Filter = filter ?? new Dictionary<string, object?>();
            DeclaredDelegate = declaredDelegate;
            DeclaredType = declaredType;

            _dispose = dispose;
        }

        public IPerperStream Subscribe()
        {
            return new PerperFabricStream(StreamName, true, Filter);
        }

        IPerperStream IPerperStream.Filter<T>(string fieldName, T value)
        {
            if (JavaTypeMappingHelper.GetJavaTypeAsString(typeof(T)) == null) {
                throw new NotImplementedException("Object filters are not supported in this version of Perper. Use dotted property names instead.");
            }
            var newFilter = new Dictionary<string, object?>(Filter);
            newFilter[fieldName] = value;
            return new PerperFabricStream(StreamName, Subscribed, newFilter);
        }

        public StreamParam AsStreamParam()
        {
            return new StreamParam(StreamName, Filter);
        }

        public ValueTask DisposeAsync()
        {
            return _dispose == null ? default : new ValueTask(_dispose());
        }

        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteDictionary("filter", Filter);
            writer.WriteString("streamName", StreamName);
            writer.WriteBoolean("subscribed", Subscribed);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            Filter = (Dictionary<string, object?>)reader.ReadDictionary("filter", s => new Dictionary<string, object?>(s));
            StreamName = reader.ReadString("streamName");
            Subscribed = reader.ReadBoolean("subscribed");
        }
    }
}