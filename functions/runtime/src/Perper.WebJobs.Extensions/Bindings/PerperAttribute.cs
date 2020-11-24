using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Bindings
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue /* HACK! */)]
    public sealed class PerperAttribute : Attribute
    {
        [AutoResolve] public string Stream { get; set; } = "{stream}";
    }
}