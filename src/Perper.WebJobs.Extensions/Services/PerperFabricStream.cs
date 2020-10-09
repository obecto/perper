using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Perper.WebJobs.Extensions.Model;
using Perper.Protocol.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IPerperStream, IBinarizable
    {
        public string StreamName { get; private set; }

        public bool Subscribed { get; private set; }

        public IBinaryObject FilterObject { get; private set; }

        public bool IsFiltered { get; private set; }

        public string DeclaredDelegate { get; }

        public Type? DeclaredType { get; }

        private Func<Task>? _dispose;

        public PerperFabricStream(string streamName, IBinaryObject filterObject, bool filtered, bool subscribed, string declaredDelegate = "", Type? declaredType = null, Func<Task>? dispose = null)
        {
            StreamName = streamName;
            Subscribed = subscribed;
            FilterObject = filterObject;
            IsFiltered = filtered;
            DeclaredDelegate = declaredDelegate;
            DeclaredType = declaredType;

            _dispose = dispose;
        }

        public IPerperStream Subscribe()
        {
            return new PerperFabricStream(StreamName, FilterObject, IsFiltered, true);
        }

        public IPerperStream Filter<T>(string fieldName, T value)
        {
            var newFilter = FilterObject.ToBuilder().SetField(fieldName, value).Build();
            return new PerperFabricStream(StreamName, newFilter, true, Subscribed);
        }

        public StreamParam AsStreamParam()
        {
            return new StreamParam(StreamName, IsFiltered ? FilterObject : null);
        }

        public ValueTask DisposeAsync()
        {
            return _dispose == null ? default : new ValueTask(_dispose());
        }

        public void WriteBinary(IBinaryWriter writer)
        {
            writer.WriteBoolean("filtered", IsFiltered);
            writer.WriteObject("filterObject", FilterObject);
            writer.WriteString("streamName", StreamName);
            writer.WriteBoolean("subscribed", Subscribed);
        }

        public void ReadBinary(IBinaryReader reader)
        {
            IsFiltered = reader.ReadBoolean("filtered");
            FilterObject = reader.ReadObject<IBinaryObject>("filterObject");
            StreamName = reader.ReadString("streamName");
            Subscribed = reader.ReadBoolean("subscribed");
        }
    }
}