using System;
using System.IO.Pipelines;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricContext
    {
        public IBinaryObject GetBinaryObject(string cacheName)
        {
            throw new NotImplementedException();
        }
        
        public PerperFabricOutput GetOutput(string cacheName)
        {
            throw new NotImplementedException();
        }

        public PerperFabricInput GetInput(string cacheName)
        {
            throw new NotImplementedException();
        }
    }
}