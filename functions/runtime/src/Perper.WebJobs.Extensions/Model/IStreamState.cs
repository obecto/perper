using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IStreamState
    {
        Task Load();
        Task Store();
    }

    public interface IStreamState<T> : IStreamState
    {
        public T Value { get; set; }
    }
}