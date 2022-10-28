using System.Reflection;

namespace Perper.Model
{
    public record PerperExecutionFilter(string Agent, string? Instance, string? Delegate)
    {
        public PerperExecutionFilter(string agent) : this(agent, null, null) { }

        public PerperExecutionFilter(PerperInstance instance) : this(instance.Agent, instance.Instance, null) { }

        public PerperExecutionFilter(string agent, string @delegate) : this(agent, null, @delegate) { }

        public PerperExecutionFilter(PerperInstance instance, string @delegate) : this(instance.Agent, instance.Instance, @delegate) { }

        public bool Reserve { get; init; } = true;
        public ParameterInfo[]? Parameters { get; init; } = null;
    }
}