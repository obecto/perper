using System.Diagnostics.CodeAnalysis;

namespace Perper.Model
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public record PerperExecution(string Execution)
    {
        public override string ToString() => $"PerperExecution({Execution})";
    }
}