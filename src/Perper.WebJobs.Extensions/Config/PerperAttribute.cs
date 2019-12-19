using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class PerperAttribute : Attribute
    {
        [AutoResolve] public string Stream { get; } = "{stream}";
        [AutoResolve] public string TriggerAttribute { get; } = "{triggerAttribute}";
        public string Parameter { get; }

        public PerperAttribute(string parameter)
        {
            Parameter = parameter;
        }
    }
}