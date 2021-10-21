using System.Diagnostics.CodeAnalysis;

namespace Perper.Protocol.Cache
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class InstanceData
    {
        private readonly string agent;

        public InstanceData(string agent) => this.agent = agent;

        public string Agent => agent;
    }
}