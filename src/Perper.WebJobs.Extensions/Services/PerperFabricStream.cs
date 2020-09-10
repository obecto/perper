using System;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IPerperStream
    {
        public string StreamName { get; }

        public bool Subscribed { get; }

        public string? FilterField { get; }

        public object? FilterValue { get; }

        public string DeclaredDelegate { get; }

        public Type? DeclaredType { get; }

        [NonSerialized]
        private Func<Task>? _dispose;

        public PerperFabricStream(string streamName, bool subscribed = false, string? filterField = null, object? filterValue = null, string declaredDelegate = "", Type? declaredType = null, Func<Task>? dispose = null)
        {
            StreamName = streamName;
            Subscribed = subscribed;
            FilterField = filterField;
            FilterValue = filterValue;
            DeclaredDelegate = declaredDelegate;
            DeclaredType = declaredType;

            _dispose = dispose;
        }

        public IPerperStream Subscribe()
        {
            return new PerperFabricStream(StreamName, true, FilterField, FilterValue);
        }

        public IPerperStream Filter(string fieldName, object value)
        {
            if (FilterField != null)
            {
                throw new NotImplementedException("Filtering on multiple fields is not supported in this version of Perper.");
            }
            return new PerperFabricStream(StreamName, Subscribed, fieldName, value);
        }

        public ValueTask DisposeAsync()
        {
            return _dispose == null ? default : new ValueTask(_dispose());
        }
    }
}