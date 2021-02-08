using System;
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
        private HashSet<ChannelWriter<int>> _channels = new HashSet<ChannelWriter<int>>();

        public FakeStream(Task<IAsyncEnumerable<T>> source)
        {
            ExecutionTask = Produce(source);
        }

        public FakeStream(IAsyncEnumerable<T> source) : this(Task.FromResult(source)) {}

        public FakeStream(IEnumerable<T> source) : this(source.ToAsyncEnumerable()) {}

        private TestStreamAsyncEnumerable GetEnumerable(Func<T, bool> filter, bool replay)
        {
            return new TestStreamAsyncEnumerable(this, filter, replay);
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return GetEnumerable((_) => true, false).GetAsyncEnumerator(cancellationToken);
        }

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

                    foreach (var channel in _channels)
                    {
                        await channel.WriteAsync(index);
                    }
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
            _channels.Add(channel.Writer);
            return channel.Reader;
        }

        public class TestStreamAsyncEnumerable : IAsyncEnumerable<T>
        {
            protected FakeStream<T> _stream;

            public Func<T, bool> Filter { get; private set; }
            public bool Replay { get; private set; }

            public TestStreamAsyncEnumerable(FakeStream<T> stream, Func<T, bool> filter, bool replay)
            {
                _stream = stream;
                Replay = replay;
                Filter = filter;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
            {
                return Impl(cancellationToken).GetAsyncEnumerator(cancellationToken);
            }

            private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                if (Replay)
                {
                    for (var i = 0; i < _stream.StoredData.Count; i++) // Not using foreach, as StoredData can change asynchronously
                    {
                        var value = FakeConfiguration.Deserialize<T>(_stream.StoredData[i]);
                        if (Filter(value))
                        {
                            // NOTE: We are not storing state entries here
                            yield return value;
                        }
                    }
                }

                // NOTE: Race condition: can miss a few elements while switching from replay to realtime

                await foreach (var i in _stream.GetChannel().ReadAllAsync())
                {
                    var value = FakeConfiguration.Deserialize<T>(_stream.StoredData[i]);

                    if (Filter(value))
                    {
                        // NOTE: We are not storing state entries here
                        yield return value;
                    }

                }
            }
        }
    }

    public class DeclaredFakeStream<T> : FakeStream<T>
    {
        public string? FunctionName;

        private readonly TaskCompletionSource<IAsyncEnumerable<T>> _source;

        public DeclaredFakeStream(TaskCompletionSource<IAsyncEnumerable<T>> source) : base(source.Task)
        {
            _source = source;
        }

        public DeclaredFakeStream() : this(new TaskCompletionSource<IAsyncEnumerable<T>>()) {}

        public void SetSource(IAsyncEnumerable<T> source)
        {
            _source.SetResult(source);
        }
    }
}