using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStream<T> : IPerperStream<T>
    {
        private readonly PerperFabricInput _input;
        private readonly string _parameterName;

        public PerperStream(PerperFabricInput input, string parameterName)
        {
            _input = input;
            _parameterName = parameterName;
        }

        public async Task Listen(Action<T> listener, CancellationToken cancellationToken = default)
        {
            await _input.Listen(o =>
                {
                    if (o.HasField(_parameterName))
                    {
                        listener(o.GetField<IBinaryObject>(_parameterName).Deserialize<T>());
                    }

                    return Task.CompletedTask;
                },
                cancellationToken);
        }
    }
}