using System;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IPerperStream
    {
        public string StreamName { get; }

        public bool Subscribed { get; }

        public string DeclaredDelegate { get; }

        [NonSerialized]
        private Func<Task>? _dispose;

        public PerperFabricStream(string streamName, bool subscribed = false, string declaredDelegate = "", Func<Task>? dispose = null)
        {
            StreamName = streamName;
            Subscribed = subscribed;
            DeclaredDelegate = declaredDelegate;

            _dispose = dispose;
        }

        public IPerperStream Subscribe()
        {
            return new PerperFabricStream(StreamName, true);
        }

        public ValueTask DisposeAsync()
        {
            return _dispose == null ? default : new ValueTask(_dispose());
        }
    }
}