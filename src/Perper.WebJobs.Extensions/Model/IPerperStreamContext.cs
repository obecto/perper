namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStreamContext<out TState>
    {
        TState State { get; }

        IPerperStream<TOut> CallStreamFunction<TOut>(string funcName, object parameters);
        IPerperStream<TOut> CallStreamFunction<TOut>(string funcName, object parameters, IPerperStream<object> affinity);
    }
}