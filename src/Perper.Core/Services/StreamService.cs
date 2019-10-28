using System;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;

namespace Perper.Core.Services
{
    [Serializable]
    public class StreamService : IService
    {
        [InstanceResource]
        private readonly IIgnite _ignite;
        
        public void Init(IServiceContext context)
        {
            throw new System.NotImplementedException();
        }

        public void Execute(IServiceContext context)
        {
             
        }

        public void Cancel(IServiceContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}