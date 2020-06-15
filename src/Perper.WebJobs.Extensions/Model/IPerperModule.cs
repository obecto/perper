namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperModule<TInput, out TOutput>
    {
        TInput Init(PerperStreamContext context, IPerperLoader loader);

        TOutput Build(PerperStreamContext context, TInput input);
    }
}