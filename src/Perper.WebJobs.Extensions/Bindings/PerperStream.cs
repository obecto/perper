using System;
using System.Threading;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStream<T> : IPerperStream<T>
    {
        private readonly PerperFabricInput _input;

        public PerperStream(PerperFabricInput input)
        {
            _input = input;
        }

        public async Task Listen(Action<T> listener, CancellationToken cancellationToken = default)
        {
            await _input.Listen(o => listener(o.Deserialize<T>()), cancellationToken);
        }
    }
}