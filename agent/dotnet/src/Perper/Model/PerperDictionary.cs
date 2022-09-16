using System.Diagnostics.CodeAnalysis;

namespace Perper.Model
{
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "We want class names to be correct for Ignite's reflection")]
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public class PerperDictionary
    {
        private readonly string name;

        public PerperDictionary(string name) => this.name = name;

        public string Name => name;

        public override string ToString() => $"PerperDictionary({Name})";
    }
}