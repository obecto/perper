using System;
using System.Linq;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Extensions
{
    public class PerperStreamBuilder
    {
        public string? Delegate { get; }

        public bool IsBlank => Delegate == null;
        public PerperStream Stream => StreamCreationLazy.Value.stream;

        private readonly PerperStreamOptions Options = new();
        private readonly DelayedCreateFunc? LinkedExecutionCreation = null;

        private readonly Lazy<(PerperStream stream, Func<Task> create)> StreamCreationLazy;

        public PerperStreamBuilder(string? @delegate)
        {
            Delegate = @delegate;
            if (Delegate != null)
            {
                var (linkedExecution, linkedExecutionCreation) = AsyncLocalContext.PerperContext.Executions.Create(AsyncLocalContext.PerperContext.CurrentAgent, Delegate);
                LinkedExecutionCreation = linkedExecutionCreation;
                StreamCreationLazy = new(() => AsyncLocalContext.PerperContext.Streams.Create(Options, linkedExecution));
            }
            else
            {
                StreamCreationLazy = new(() => AsyncLocalContext.PerperContext.Streams.Create(Options));
            }
        }

        public async Task<PerperStream> StartAsync(params object[] parameters)
        {
            await StreamCreationLazy.Value.create().ConfigureAwait(false);

            if (LinkedExecutionCreation != null)
            {
                await LinkedExecutionCreation(parameters).ConfigureAwait(false);
            }
            else if (parameters.Length > 0)
            {
                throw new InvalidOperationException("PerperStreamBuilder.StartAsync() does not take parameters for blank streams");
            }

            return StreamCreationLazy.Value.stream;
        }

        public PerperStreamBuilder Persistent() => Configure(options => options.Persistent = true);
        public PerperStreamBuilder Ephemeral() => Configure(options => options.Persistent = false);
        public PerperStreamBuilder Packed(long stride) => Configure(options => options.Stride = stride);
        public PerperStreamBuilder Action() => Configure(options => options.Action = true);
        public PerperStreamBuilder Index(Type type) => Configure(options => options.IndexTypes = options.IndexTypes.Append(type));
        public PerperStreamBuilder Index<T>() => Index(typeof(T));

        public PerperStreamBuilder Configure(Action<PerperStreamOptions> change)
        {
            if (StreamCreationLazy.IsValueCreated)
            {
                throw new InvalidOperationException("PerperStreamBuilder has already been initialized.");
            }
            change(Options);
            return this;
        }
    }
}