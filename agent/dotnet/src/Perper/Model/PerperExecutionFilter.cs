namespace Perper.Model
{
    public record PerperExecutionFilter(string Agent, string? Instance, string? Delegate)
    {
        public PerperExecutionFilter(string agent) : this(agent, null, null) { }

        public PerperExecutionFilter(PerperAgent agent) : this(agent.Agent, agent.Instance, null) { }

        public PerperExecutionFilter(string agent, string @delegate) : this(agent, null, @delegate) { }

        public PerperExecutionFilter(PerperAgent agent, string @delegate) : this(agent.Agent, agent.Instance, @delegate) { }
    }
}