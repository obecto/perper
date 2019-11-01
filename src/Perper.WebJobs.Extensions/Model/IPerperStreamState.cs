using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStreamState
    {
        T Get<T>(string name);
        Task PutAsync<T>(string name, T value);
    }
}