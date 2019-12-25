using System;
using System.Threading.Tasks;
using Apache.Ignite.Core;

namespace Perper.Fabric.Services
{
    public class StreamServiceDeployment : IDisposable
    {
        private readonly IIgnite _ignite;
        private readonly string _name;

        public StreamServiceDeployment(IIgnite ignite, string name)
        {
            _ignite = ignite;
            _name = name;
        }

        public async Task DeployAsync()
        {
            var refCount = _ignite.GetAtomicLong(_name, 0, true);
            
            if (refCount.Increment() == 1)
            {
                var service = new StreamService {StreamObjectTypeName = _name};
                await _ignite.GetServices().DeployClusterSingletonAsync(_name, service);
            }
        }

        public void Dispose()
        {
            var refCount = _ignite.GetAtomicLong(_name, 0, true);
            if (refCount.Decrement() == 0)
            {
                _ignite.GetServices().Cancel(_name);
            }
        }
    }
}