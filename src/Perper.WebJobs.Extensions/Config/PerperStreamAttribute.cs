using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperStreamAttribute : Attribute
    {
        [AutoResolve] public string Stream { get; set; } = "{stream}";
        [AutoResolve] public string TriggerAttribute { get; set; } = "{triggerAttribute}";
        public string Parameter { get; }

        public PerperStreamAttribute(string parameter)
        {
            Parameter = parameter;
        }
    }
}