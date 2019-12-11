using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStreamContext
    {
        Task CallStreamAction(string name, object parameters);
        Task<IPerperStreamHandle> CallStreamFunction(string name, object parameters);

        T GetState<T>(string name);
        Task SaveState<T>(string name, T state);

        Task<T> CallWorkerFunction<T>(object parameters);
    }
}