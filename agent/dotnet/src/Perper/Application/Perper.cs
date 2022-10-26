using System.Diagnostics.CodeAnalysis;

using Perper.Model;

namespace Perper.Application
{
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instanciated via reflection")]
    internal class Perper : IPerper
    {
        public Perper(IPerperExecutions executions, IPerperAgents agents, IPerperStreams streams, IPerperStates states)
        {
            Executions = executions;
            Agents = agents;
            Streams = streams;
            States = states;
        }

        public IPerperExecutions Executions { get; }
        public IPerperAgents Agents { get; }
        public IPerperStreams Streams { get; }
        public IPerperStates States { get; }
    }
}