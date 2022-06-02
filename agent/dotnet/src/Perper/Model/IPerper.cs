namespace Perper.Model
{
    public interface IPerper
    {
        IPerperExecutions Executions { get; }
        IPerperAgents Agents { get; }
        IPerperStreams Streams { get; }
        IPerperStates States { get; }
    }
}