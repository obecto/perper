using System;

namespace Perper.WebJobs.Extensions.Config
{
    public class PerperTriggerAttribute : Attribute
    {
        public string Stream { get; }
        public string Parameter { get; }

        public PerperTriggerAttribute(string stream, string parameter = "context")
        {
            Stream = stream;
            Parameter = parameter;
        } 
    }
}