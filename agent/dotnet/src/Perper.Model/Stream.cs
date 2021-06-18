using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Extensions;


namespace Perper.Model
{
    public class Stream : IStream
    {
        public Stream(PerperStream rawStream)
        {
            RawStream = rawStream;
        }

        public PerperStream RawStream { get; }
    }

    public class Stream<T> : Stream, IStream<T>
    {
        public Stream(PerperStream rawStream) : base(rawStream) { }

        public IAsyncEnumerable<T> DataLocal()
        {
            return new Stream<T>(new PerperStream(RawStream.Stream, RawStream.Filter, RawStream.Replay, true));
        }

        public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            return new Stream<T>(new PerperStream(RawStream.Stream, FilterUtils.ConvertFilter(filter), RawStream.Replay, dataLocal));
        }

        public IAsyncEnumerable<T> Replay(bool dataLocal = false)
        {
            return new Stream<T>(new PerperStream(RawStream.Stream, RawStream.Filter, true, dataLocal));
        }

        public IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            return new Stream<T>(new PerperStream(RawStream.Stream, FilterUtils.ConvertFilter(filter), true, dataLocal));
        }

        public IQueryable<T> Query()
        {
            return AsyncLocals.CacheService.StreamGetQueryable<T>(RawStream.Stream);
        }

        public async IAsyncEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> queryFunc) // TODO: Convert to extension method on IQueryable
        {
            var queryable = queryFunc(Query());

            using var enumerator = queryable.GetEnumerator();
            while (await Task.Run(enumerator.MoveNext)) // Blocking, should run in background
            {
                yield return enumerator.Current;
            }
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return Impl(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int parameter = 42; // FIXME
            var listener = await AsyncLocals.CacheService.StreamAddListener(RawStream, AsyncLocals.Agent, AsyncLocals.Instance, parameter);
            try
            {
                await foreach (var (key, notification) in AsyncLocals.NotificationService.GetNotifications(AsyncLocals.Instance, parameter, cancellationToken))
                {
                    if (notification is StreamItemNotification si)
                    {
                        var value = await AsyncLocals.CacheService.StreamReadItem<T>(si);

                        // await ((State)_stream._state).LoadStateEntries();

                        try
                        {
                            yield return value;
                        }
                        finally
                        {
                            // await ((State)_stream._state).StoreStateEntries();

                            await AsyncLocals.NotificationService.ConsumeNotification(key);
                        }
                    }
                }
            }
            finally
            {
                if (parameter < 0)
                {
                    await AsyncLocals.CacheService.StreamRemoveListener(RawStream, listener);
                }
            }
        }
    }
}