using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Bindings
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperStreamAttribute : Attribute
    {
        public PerperStreamAttribute(string parameterName = "output")
        {
            
        }
    }
}