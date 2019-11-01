using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStreamContext
    {
        Task CallStreamAction(string funcName, object parameters);
        Task<IPerperStreamHandle> CallStreamFunction(string funcName, object parameters);
    }
}