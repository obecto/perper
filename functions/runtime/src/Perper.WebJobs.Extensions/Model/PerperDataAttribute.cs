using System;

namespace Perper.WebJobs.Extensions.Model
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class PerperDataAttribute : Attribute
    {
        public string? Name;
    }
}