using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Triggers
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class PerperTriggerAttribute : Attribute
    {
        public string? ParameterExpression { get; set; }
    }
}