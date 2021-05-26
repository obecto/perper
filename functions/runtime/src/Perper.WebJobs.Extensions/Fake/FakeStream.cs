using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Fake
{
    public class FakeStream : IStream
    {
        public Task? ExecutionTask { get; set; }
    }

    public class FakeStream<T> : FakeStream, IStream<T>
    {
        private List<object?> StoredData { get; set; } = new List<object?>();
        private readonly ConcurrentDictionary<ChannelWriter<int>, bool> _channels = new ConcurrentDictionary<ChannelWriter<int>, bool>();
        private bool _finished;

        public FakeStream(Task<IAsyncEnumerable<T>> source) => ExecutionTask = Produce(source);

        public FakeStream(IAsyncEnumerable<T> source) : this(Task.FromResult(source)) { }

        public FakeStream(IEnumerable<T> source) : this(source.ToAsyncEnumerable()) { }

        private FakeStreamAsyncEnumerable GetEnumerable(Func<T, bool> filter, bool replay) => new FakeStreamAsyncEnumerable(this, filter, replay);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => GetEnumerable((_) => true, false).GetAsyncEnumerator(cancellationToken);

        public IAsyncEnumerable<T> DataLocal()
            => GetEnumerable((_) => true, false);

        public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false)
            => GetEnumerable(filter.Compile(), false);

        public IAsyncEnumerable<T> Replay(bool dataLocal = false)
            => GetEnumerable((_) => true, true);

        public IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false)
            => GetEnumerable(filter.Compile(), true);

        public IAsyncEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query)
        {
            var source = StoredData.AsQueryable().Select(x => FakeConfiguration.Deserialize<T>(x));
            var result = query(source);
            return result.ToAsyncEnumerable();
        }

        private async Task Produce(Task<IAsyncEnumerable<T>> source)
        {
            try
            {
                await foreach (var item in await source)
                {
                    var index = StoredData.Count;
                    StoredData.Add(FakeConfiguration.Serialize(item));

                    foreach (var channel in _channels.Keys)
                    {
                        await channel.WriteAsync(index);
                    }
                }

                _finished = true;
                foreach (var channel in _channels.Keys)
                {
                    channel.Complete();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while executing stream: {0}", e);
                throw;
            }
        }

        private ChannelReader<int> GetChannel()
        {
            var channel = Channel.CreateUnbounded<int>();
            if (!_finished)
            {
                _channels[channel.Writer] = true;
            }
            else
            {
                channel.Writer.Complete();
            }
            return channel.Reader;
        }

        public class FakeStreamAsyncEnumerable : IAsyncEnumerable<T>
        {
            protected FakeStream<T> _stream;

            public Func<T, bool> Filter { get; private set; }
            public bool Replay { get; private set; }

            public FakeStreamAsyncEnumerable(FakeStream<T> stream, Func<T, bool> filter, bool replay)
            {
                _stream = stream;
                Replay = replay;
                Filter = filter;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) => Impl(cancellationToken).GetAsyncEnumerator(cancellationToken);

            private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                if (Replay)
                {
                    for (var i = 0; i < _stream.StoredData.Count; i++) // Not using foreach, as StoredData can change asynchronously
                    {
                        var value = FakeConfiguration.Deserialize<T>(_stream.StoredData[i]);
                        if (Filter(value))
                        {
                            // NOTE: We are not saving state entries here
                            yield return value;
                        }
                    }
                }

                // NOTE: Race condition: can miss elements while switching from replay to realtime

                await foreach (var i in _stream.GetChannel().ReadAllAsync(cancellationToken))
                {
                    var value = FakeConfiguration.Deserialize<T>(_stream.StoredData[i]);

                    if (Filter(value))
                    {
                        // NOTE: We are not saving state entries here
                        yield return value;
                    }

                }
            }
        }
    }

    public class DeclaredFakeStream<T> : FakeStream<T>
    {
        public string? FunctionName;

        private readonly TaskCompletionSource<Task<IAsyncEnumerable<T>>> _source;

        public DeclaredFakeStream(TaskCompletionSource<Task<IAsyncEnumerable<T>>> source) : base(source.Task.Unwrap()) => _source = source;

        public DeclaredFakeStream() : this(new TaskCompletionSource<Task<IAsyncEnumerable<T>>>()) { }

        public void SetSource(Task<IAsyncEnumerable<T>> source) => _source.SetResult(source);
    }
}