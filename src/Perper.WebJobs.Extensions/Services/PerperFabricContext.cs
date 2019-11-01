using System;
using System.IO.Pipelines;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricContext
    {
        public PerperFabricOutput GetOutput(string funcName)
        {
            throw  new NotImplementedException();
        }

        public PerperFabricInput GetInput(string funcName)
        {
            throw new NotImplementedException();
        }
    }
}