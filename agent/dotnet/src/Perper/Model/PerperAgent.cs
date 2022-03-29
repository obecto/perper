using System.Diagnostics.CodeAnalysis;

namespace Perper.Model
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class PerperAgent
    {
        private readonly string agent;
        private readonly string instance;

        public PerperAgent(
            string agent,
            string instance)
        {
            this.agent = agent;
            this.instance = instance;
        }

        public string Agent => agent;
        public string Instance => instance;

        public override string ToString() => $"PerperAgent({Agent}, {Instance})";
    }
}