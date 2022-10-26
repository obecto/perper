namespace Perper.Model
{
    public interface IPerperContext : IPerper
    {
        PerperExecution CurrentExecution { get; }
        PerperInstance CurrentAgent { get; }
        PerperDictionary CurrentState { get; }
    }
}