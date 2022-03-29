using System.Diagnostics.CodeAnalysis;

using Perper.Model;

namespace Perper.Application
{
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instanciated via reflection")]
    internal class PerperContext : IPerperContext
    {
        private readonly IPerper Perper;
        public PerperExecution CurrentExecution { get; }
        public PerperAgent CurrentAgent { get; }
        public PerperState CurrentState { get; }

        public PerperContext(IPerper perper, PerperExecutionData data)
        {
            Perper = perper;
            CurrentExecution = data.Execution;
            CurrentAgent = data.Agent;
            CurrentState = States.Create(data.Agent);
        }

        public IPerperExecutions Executions => Perper.Executions;
        public IPerperAgents Agents => Perper.Agents;
        public IPerperStreams Streams => Perper.Streams;
        public IPerperStates States => Perper.States;
    }
}