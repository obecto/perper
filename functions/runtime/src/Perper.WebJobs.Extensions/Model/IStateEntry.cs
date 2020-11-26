using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IStateEntry<T>
    {
        public Task Load();
        public Task Store();
        public T Value { get; set; }
    }
}