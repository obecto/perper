using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStreamContext
    {
        T GetState<T>() where T : new();
        Task SaveState();
        Task<T> CallWorkerFunction<T>(object parameters);

        Task CallStreamAction(string name, object parameters);
        Task<IPerperStreamHandle> CallStreamFunction(string name, object parameters);
    }
}