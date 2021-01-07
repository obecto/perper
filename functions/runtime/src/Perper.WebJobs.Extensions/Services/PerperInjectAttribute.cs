using System;
using Microsoft.Azure.WebJobs.Description;

namespace Perper.WebJobs.Extensions.Services
{
    [Binding]
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class PerperInjectAttribute : Attribute
    {
    }
}