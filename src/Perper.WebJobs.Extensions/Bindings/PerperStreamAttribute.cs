using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Bindings
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperStreamAttribute : Attribute
    {
        public string FunctionName { get; }
        public string ParameterName { get; }

        public PerperStreamAttribute(string functionName = default, string parameterName = "output")
        {
            FunctionName = functionName;
            ParameterName = parameterName;
        }
    }
}