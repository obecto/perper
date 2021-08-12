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
        public Stream(PerperStream rawStream) => RawStream = rawStream;

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

        public async IAsyncEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query) // TODO: Convert to extension method on IQueryable
        {
            var queryable = query(Query());

            using var enumerator = queryable.GetEnumerator();
            while (await Task.Run(enumerator.MoveNext).ConfigureAwait(false)) // Blocking, should run in background
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
            var random = new Random();
            var parameter = random.Next(1, 1000000);
            //int parameter = 42; // FIXME
            var listener = await AsyncLocals.CacheService.StreamAddListener(RawStream, AsyncLocals.Agent, AsyncLocals.Execution, parameter).ConfigureAwait(false);
            try
            {
                await foreach (var (key, notification) in AsyncLocals.NotificationService.GetNotifications(AsyncLocals.Execution, parameter, cancellationToken))
                {
                    if (notification is StreamItemNotification si)
                    {
                        T? value;

                        try
                        {
                            value = await AsyncLocals.CacheService.StreamReadItem<T>(si).ConfigureAwait(false);
                        }
                        catch (KeyNotFoundException)
                        {
                            try
                            {
                                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                                value = await AsyncLocals.CacheService.StreamReadItem<T>(si).ConfigureAwait(false);
                            }
                            catch (KeyNotFoundException)
                            {
                                continue;
                            }
                        }

                        // await ((State)_stream._state).LoadStateEntries();

                        try
                        {
                            yield return value;
                        }
                        finally
                        {
                            // await ((State)_stream._state).StoreStateEntries();

                            await AsyncLocals.NotificationService.ConsumeNotification(key).ConfigureAwait(false);
                        }
                    }
                }
            }
            finally
            {
                if (parameter < 0)
                {
                    await AsyncLocals.CacheService.StreamRemoveListener(RawStream, listener).ConfigureAwait(false);
                }
            }
        }
    }
}