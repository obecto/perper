using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
        public bool KeepBinary { get; }

        public Stream(PerperStream rawStream, bool keepBinary = false) : base(rawStream) => KeepBinary = keepBinary;

        public IAsyncEnumerable<T> DataLocal()
        {
            return new Stream<T>(new PerperStream(RawStream.Stream, RawStream.Filter, RawStream.Replay, true), KeepBinary);
        }

        public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            return new Stream<T>(new PerperStream(RawStream.Stream, FilterUtils.ConvertFilter(filter), RawStream.Replay, dataLocal), KeepBinary);
        }

        public IAsyncEnumerable<T> Replay(bool dataLocal = false)
        {
            return new Stream<T>(new PerperStream(RawStream.Stream, RawStream.Filter, true, dataLocal), KeepBinary);
        }

        public IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            return new Stream<T>(new PerperStream(RawStream.Stream, FilterUtils.ConvertFilter(filter), true, dataLocal), KeepBinary);
        }

        public IQueryable<T> Query()
        {
            return AsyncLocals.CacheService.StreamGetQueryable<T>(RawStream.Stream, KeepBinary);
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

        public IAsyncEnumerable<T> Query(string typeName, string sqlCondition, object[] sqlParameters)
        {
            return AsyncLocals.CacheService.StreamQuerySql<T>(RawStream.Stream, $"select _VAL from {typeName.ToUpper()} {sqlCondition}", sqlParameters, KeepBinary);
        }

        public IAsyncEnumerable<T> Query(string sqlCondition, object[] sqlParameters)
        {
            return Query(typeof(T).Name, sqlCondition, sqlParameters);
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
            var listener = await AsyncLocals.CacheService.StreamAddListener(RawStream, AsyncLocals.Agent, AsyncLocals.Instance, AsyncLocals.Execution, parameter).ConfigureAwait(false);
            try
            {
                await foreach (var (key, notification) in AsyncLocals.NotificationService.GetStreamItemNotifications(AsyncLocals.Execution, parameter).ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        T? value;

                        try
                        {
                            value = await AsyncLocals.CacheService.StreamReadItem<T>(notification, KeepBinary).ConfigureAwait(false);
                        }
                        catch (KeyNotFoundException)
                        {
                            try
                            {
                                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                                value = await AsyncLocals.CacheService.StreamReadItem<T>(notification, KeepBinary).ConfigureAwait(false);
                            }
                            catch (KeyNotFoundException)
                            {
                                continue;
                            }
                        }

                        // await ((State)_stream._state).LoadStateEntries();

                        yield return value;
                    }
                    finally
                    {
                        // await ((State)_stream._state).StoreStateEntries();

                        await AsyncLocals.NotificationService.ConsumeNotification(key).ConfigureAwait(false);
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