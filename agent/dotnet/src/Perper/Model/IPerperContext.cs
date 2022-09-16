namespace Perper.Model
{
    public interface IPerperContext : IPerper
    {
        PerperExecution CurrentExecution { get; }
        PerperAgent CurrentAgent { get; }
        PerperDictionary CurrentState { get; }
    }
}