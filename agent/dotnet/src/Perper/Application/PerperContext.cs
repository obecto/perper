using System.Diagnostics.CodeAnalysis;

using Perper.Model;

namespace Perper.Application
{
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instanciated via reflection")]
    internal class PerperContext : IPerperContext
    {
        private readonly IPerper _perper;
        public PerperExecution CurrentExecution { get; }
        public PerperAgent CurrentAgent { get; }
        public PerperState CurrentState { get; }

        public PerperContext(IPerper perper, PerperExecutionData data)
        {
            _perper = perper;
            CurrentExecution = data.Execution;
            CurrentAgent = data.Agent;
            CurrentState = States.Create(data.Agent);
        }

        public IPerperExecutions Executions => _perper.Executions;
        public IPerperAgents Agents => _perper.Agents;
        public IPerperStreams Streams => _perper.Streams;
        public IPerperStates States => _perper.States;
    }
}