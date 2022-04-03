using System.Diagnostics.CodeAnalysis;

namespace Perper.Model
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public class PerperExecution
    {
        private readonly string execution;

        public PerperExecution(string execution) => this.execution = execution;

        public string Execution => execution;

        public override string ToString() => $"PerperExecution({Execution})";
    }
}