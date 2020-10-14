using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperModuleTriggerAttribute : Attribute
    {
        public bool RunOnStartup { get; set; }
    }
}