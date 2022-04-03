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
        public PerperStream Stream => _streamCreationLazy.Value.stream;

        private readonly PerperStreamOptions _options = new();
        private readonly DelayedCreateFunc? _linkedExecutionCreation = null;

        private readonly Lazy<(PerperStream stream, Func<Task> create)> _streamCreationLazy;

        public PerperStreamBuilder(string? @delegate)
        {
            Delegate = @delegate;
            if (Delegate != null)
            {
                var (linkedExecution, linkedExecutionCreation) = AsyncLocalContext.PerperContext.Executions.Create(AsyncLocalContext.PerperContext.CurrentAgent, Delegate);
                _linkedExecutionCreation = linkedExecutionCreation;
                _streamCreationLazy = new(() => AsyncLocalContext.PerperContext.Streams.Create(_options, linkedExecution));
            }
            else
            {
                _streamCreationLazy = new(() => AsyncLocalContext.PerperContext.Streams.Create(_options));
            }
        }

        public async Task<PerperStream> StartAsync(params object[] parameters)
        {
            await _streamCreationLazy.Value.create().ConfigureAwait(false);

            if (_linkedExecutionCreation != null)
            {
                await _linkedExecutionCreation(parameters).ConfigureAwait(false);
            }
            else if (parameters.Length > 0)
            {
                throw new InvalidOperationException("PerperStreamBuilder.StartAsync() does not take parameters for blank streams");
            }

            return _streamCreationLazy.Value.stream;
        }

        public PerperStreamBuilder Persistent() => Configure(options => options.Persistent = true);
        public PerperStreamBuilder Ephemeral() => Configure(options => options.Persistent = false);
        public PerperStreamBuilder Packed(long stride) => Configure(options => options.Stride = stride);
        public PerperStreamBuilder Action() => Configure(options => options.Action = true);
        public PerperStreamBuilder Index(Type type) => Configure(options => options.IndexTypes = options.IndexTypes.Append(type));
        public PerperStreamBuilder Index<T>() => Index(typeof(T));

        public PerperStreamBuilder Configure(Action<PerperStreamOptions> change)
        {
            if (_streamCreationLazy.IsValueCreated)
            {
                throw new InvalidOperationException("PerperStreamBuilder has already been initialized.");
            }
            change(_options);
            return this;
        }
    }
}