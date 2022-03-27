using System.Diagnostics.CodeAnalysis;

using Perper.Extensions;

namespace Perper.Model
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class PerperAgent
    {
        private readonly string agent;
        private readonly string instance;
        private readonly PerperCollection<PerperAgent> children;

        public PerperAgent(
            string agent,
            string instance)
        {
            this.agent = agent;
            this.instance = instance;

            children = new PerperCollection<PerperAgent>("children");
        }

        public string Agent => agent;
        public string Instance => instance;

        public PerperCollection<PerperAgent> Children => children;

        public override string ToString() => $"PerperAgent({Agent}, {Instance})";
    }
}