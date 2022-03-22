using Perper.Protocol;

namespace Perper.Application
{
    public class PerperScopeService
    {
        public FabricExecution? CurrentExecution { get; private set; }

        public void SetExecution(FabricExecution execution)
        {
            CurrentExecution = execution;
        }
    }
}