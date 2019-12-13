using System;
using Microsoft.Azure.WebJobs.Description;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperStreamAttribute : PerperTriggerAttribute
    {
        public PerperStreamAttribute(string stream)
        {
            Stream = stream;
            FunctionType = PerperFunctionType.Stream;
        }
    }
}