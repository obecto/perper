using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Bindings
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class PerperStreamAttribute : Attribute
    {
        public PerperStreamAttribute(string parameterName = default)
        {
        }
    }
}