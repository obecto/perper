using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class PerperAttribute : Attribute
    {
        public string Parameter { get; }

        [AutoResolve] public string Stream { get; set; } = "{stream}";
        [AutoResolve] public string Worker { get; set; } = "{worker}";
        [AutoResolve] public string TriggerAttribute { get; set; } = "{triggerAttribute}";

        public PerperAttribute(string parameter)
        {
            Parameter = parameter;
        }
    }
}