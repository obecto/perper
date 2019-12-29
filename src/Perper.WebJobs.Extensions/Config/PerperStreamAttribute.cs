using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperStreamAttribute : Attribute
    {
        public string Parameter { get; }

        [AutoResolve] public string Stream { get; set; } = "{stream}";
        [AutoResolve] public string Delegate { get; set; } = "{delegate}";
        
        public PerperStreamAttribute(string parameter)
        {
            Parameter = parameter;
        }
    }
}