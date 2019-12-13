using System;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Config
{
    public abstract class PerperTriggerAttribute : Attribute
    {
        public string Stream { get; protected set; }
        public PerperFunctionType FunctionType { get; protected set; }
    }
}