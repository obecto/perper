using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperModule<TInput, TOutput>
    {
        TInput Init(PerperStreamContext context, IPerperLoader loader);

        Task<TOutput> Build(PerperStreamContext context, TInput input);
    }
}