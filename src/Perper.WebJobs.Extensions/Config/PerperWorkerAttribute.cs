using System;
using Microsoft.Azure.WebJobs.Description;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperWorkerAttribute : PerperTriggerAttribute
    {
        public PerperWorkerAttribute(string stream)
        {
            Stream = stream;
            FunctionType = PerperFunctionType.Worker;
        }
    }
}