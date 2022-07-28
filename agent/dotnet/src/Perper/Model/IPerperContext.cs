namespace Perper.Model
{
    public interface IPerperContext : IPerper
    {
        PerperExecution CurrentExecution { get; }
        PerperAgent CurrentAgent { get; }
        PerperState CurrentState { get; }
    }
}