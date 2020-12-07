using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Services
{
    [Binding]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PerperInjectAttribute : Attribute
    {
    }
}