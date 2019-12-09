using System;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStreamContext
    {
        Task CallStreamAction(string name, object parameters);
        Task<IPerperStreamHandle> CallStreamFunction<T>(string name, object parameters);

//        Task<T> GetState<T>(string key) where T:new();
//        Task SetState<T>(string key, T state);
    }
}