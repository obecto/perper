using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Model;

namespace Perper.Application.Handlers
{
    public class StreamPerperHandler : IPerperHandler<PerperStream>
    {
        private readonly PerperStreamOptions StreamOptions;
        private readonly IPerper Perper;
        private readonly string InternalDelegate;

        public StreamPerperHandler(PerperStreamOptions streamOptions, string internalDelegate, IServiceProvider services)
        {
            StreamOptions = streamOptions;
            InternalDelegate = internalDelegate;
            Perper = services.GetRequiredService<IPerper>();
        }

        public ParameterInfo[]? GetParameters() => null;

        public async Task<PerperStream> Invoke(PerperExecutionData executionData, object?[] arguments)
        {
            var (internalExecution, startAsync) = Perper.Executions.Create(executionData.Agent, InternalDelegate);

            // NOTE: If decoupling streams and executions, can create the stream before the execution.
            var stream = await Perper.Streams.CreateAsync(StreamOptions, internalExecution).ConfigureAwait(false);

            arguments = new object?[] { stream }.Concat(arguments).ToArray();
            await startAsync(arguments).ConfigureAwait(false);

            return stream;
        }
    }
}