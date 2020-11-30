using System;

namespace Perper.WebJobs.Extensions.Config
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Assembly)]
    public sealed class PerperDataAttribute : Attribute
    {
    }
}