using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperAttribute : Attribute
    {
        public string Stream { get; set; }
        public string Parameter { get; }
        public bool State { get; set; }

        public PerperAttribute(string parameter = "context")
        {
            Parameter = parameter;
        } 
    }
}