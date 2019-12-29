using System;
using Microsoft.Azure.WebJobs.Description;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperWorkerTriggerAttribute : Attribute
    {
        public string StreamDelegate { get; }
        
        public PerperWorkerTriggerAttribute(string streamDelegate)
        {
            StreamDelegate = streamDelegate;
        }
    }
}