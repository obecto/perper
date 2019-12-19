using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperStreamAttribute : Attribute
    {
        public string Stream { get; }

        public bool RunOnStartup { get; set; } = false;

        public PerperStreamAttribute(string stream)
        {
            Stream = stream;
        }
    }
}