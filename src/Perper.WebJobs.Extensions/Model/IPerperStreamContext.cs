using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStreamContext
    {
        Task CallStreamAction(string actionName, object parameters);
        Task<IPerperStreamHandle> CallStreamFunction<T>(string functionName, object parameters);
        
    }
}