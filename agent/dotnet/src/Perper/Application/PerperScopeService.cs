using System.Diagnostics.CodeAnalysis;

using Perper.Model;

namespace Perper.Application
{
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instanciated via reflection")]
    internal class PerperScopeService
    {
        public PerperExecutionData? CurrentExecution { get; private set; }

        public void SetExecution(PerperExecutionData execution)
        {
            CurrentExecution = execution;
        }
    }
}