using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Triggers
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class StreamTriggerAttribute : Attribute
    {
        
    }
}