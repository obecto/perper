using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Perper.WebJobs.Extensions.Fake
{
    public partial class FakeStream<T>
    {
        public class FakeStreamAsyncEnumerable : IAsyncEnumerable<T>
        {
            private readonly FakeStream<T> _stream;

            public Func<T, bool> Filter { get; private set; }
            public bool Replay { get; private set; }

            public FakeStreamAsyncEnumerable(FakeStream<T> stream, Func<T, bool> filter, bool replay)
            {
                _stream = stream;
                Replay = replay;
                Filter = filter;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) => Impl().GetAsyncEnumerator(cancellationToken);

            private async IAsyncEnumerable<T> Impl()
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

                await foreach (var i in _stream.GetChannel().ReadAllAsync())
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
}