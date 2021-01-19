using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Test
{
    public class TestStream : IStream
    {
        public string StreamName { get; protected set; }

        public TestStream(string streamName)
        {
            StreamName = streamName;
        }
    }

    public abstract class TestStream<T> : TestStream, IStream<T>
    {
        public List<object?> StoredData { get; set; } = new List<object?>();

        protected readonly TestEnvironment _environment;

        private HashSet<ChannelWriter<int>> _channels = new HashSet<ChannelWriter<int>>();
        private Task? _producerTask = null;

        public TestStream(string streamName, TestEnvironment environment)
            : base(streamName)
        {
            _environment = environment;
        }

        private TestStreamAsyncEnumerable GetEnumerable(Func<T, bool> filter, bool replay)
        {
            return new TestStreamAsyncEnumerable(this, filter, replay);
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return GetEnumerable((_) => true, false).GetAsyncEnumerator(cancellationToken);
        }

        public IAsyncEnumerable<T> DataLocal()
        {
            return GetEnumerable((_) => true, false);
        }

        public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            return GetEnumerable(filter.Compile(), false);
        }

        public IAsyncEnumerable<T> Replay(bool dataLocal = false)
        {
            return GetEnumerable((_) => true, true);
        }

        public IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            return GetEnumerable(filter.Compile(), true);
        }

#pragma warning disable CS1998
        public async IAsyncEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query)
        {
            foreach (var value in query(StoredData.AsQueryable().Select(x => _environment.Deserialize<T>(x))))
            {
                yield return value;
            }
        }
#pragma warning restore CS1998

        private ChannelReader<int> GetChannel()
        {
            if (_producerTask == null)
            {
                _producerTask = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var item in Produce())
                        {
                            var index = StoredData.Count;
                            StoredData.Add(_environment.Serialize(item));

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
                });
            }

            var channel = Channel.CreateUnbounded<int>();
            _channels.Add(channel.Writer);
            return channel.Reader;
        }

        protected abstract IAsyncEnumerable<T> Produce();

        public class TestStreamAsyncEnumerable : IAsyncEnumerable<T>
        {
            protected TestStream<T> _stream;

            public Func<T, bool> Filter { get; private set; }
            public bool Replay { get; private set; }

            public TestStreamAsyncEnumerable(TestStream<T> stream, Func<T, bool> filter, bool replay)
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
                        var value = _stream._environment.Deserialize<T>(_stream.StoredData[i]);
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
                    var value = _stream._environment.Deserialize<T>(_stream.StoredData[i]);

                    if (Filter(value))
                    {
                        // NOTE: We are not storing state entries here
                        yield return value;
                    }

                }
            }
        }
    }

    public class FunctionTestStream<T> : TestStream<T>
    {
        public TestInstance TestInstance { get; protected set; }
        public string FunctionDelegate { get; protected set; }
        public object? Parameters { get; set; }

        public FunctionTestStream(string streamName, TestEnvironment environment, TestInstance testInstance, string functionDelegate, object? parameters)
            : base(streamName, environment)
        {
            TestInstance = testInstance;
            FunctionDelegate = functionDelegate;
            Parameters = parameters;
        }

        protected override async IAsyncEnumerable<T> Produce()
        {
            var producedData = await _environment.CallAsync<object?, IAsyncEnumerable<T>>(TestInstance, FunctionDelegate, Parameters);
            await foreach (var item in producedData)
            {
                yield return item;
            }
        }
    }

    public class AsyncEnumerableTestStream<T> : TestStream<T>
    {
        private IAsyncEnumerable<T> _source;

        public AsyncEnumerableTestStream(string streamName, TestEnvironment environment, IAsyncEnumerable<T> source)
            : base(streamName, environment)
        {
            _source = source;
        }

        protected override IAsyncEnumerable<T> Produce() => _source;
    }
}