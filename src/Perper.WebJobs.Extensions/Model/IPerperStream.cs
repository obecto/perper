using System;
using System.Threading;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStream<out T>
    {
        Task Listen(Action<T> listener, CancellationToken cancellationToken = default);
    }
}