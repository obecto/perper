using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class PerperAttribute : Attribute
    {
        public string Stream { get; }
        public string Parameter { get; }

        public PerperAttribute(string parameter = "$return")
        {
            Parameter = parameter;
        } 
    }
}