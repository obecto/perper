using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Perper.Extensions.Collections;

//using Perper.Extensions.Collections;

namespace Perper.Model
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class PerperAgent
    {
        private readonly string agent;
        private readonly string instance;
        private readonly PerperDictionary<string, string> children;

        public PerperAgent(
            string agent,
            string instance)
        {
            this.agent = agent;
            this.instance = instance;

            children = new PerperDictionary<string, string>(instance, "children");
        }

        public string Agent => agent;
        public string Instance => instance;

        public PerperDictionary<string, string> Children => children;

        public override string ToString() => $"PerperAgent({Agent}, {Instance}, {Children})";
    }
}