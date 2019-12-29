using System;
using Microsoft.Azure.WebJobs.Description;

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