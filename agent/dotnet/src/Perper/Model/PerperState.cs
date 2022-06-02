using System.Diagnostics.CodeAnalysis;

namespace Perper.Model
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class PerperState
    {
        private readonly string name;

        public PerperState(string name) => this.name = name;

        public string Name => name;

        public override string ToString() => $"PerperState({Name})";
    }
}