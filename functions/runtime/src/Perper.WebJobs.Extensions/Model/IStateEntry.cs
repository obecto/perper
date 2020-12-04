using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IStateEntry<T>
    {
        Task Load();
        Task Store();
        T Value { get; set; }
    }
}