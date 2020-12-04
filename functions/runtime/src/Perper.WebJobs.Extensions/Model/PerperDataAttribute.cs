using System;

namespace Perper.WebJobs.Extensions.Model
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Assembly)]
    public sealed class PerperDataAttribute : Attribute
    {
    }
}