using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class PerperStreamContext
    {
        public string StreamName { get; }
        public string DelegateName { get; }

        private readonly IPerperFabricContext _context;

        public PerperStreamContext(string streamName, string delegateName, IPerperFabricContext context)
        {
            StreamName = streamName;
            DelegateName = delegateName;

            _context = context;
        }

        public IQueryable<T> Query<T>(IAsyncEnumerable<T> stream)
        {
            var streamName = (stream as PerperStreamAsyncEnumerable<T>)!.GetStreamName();
            var data = _context.GetData(streamName);

            return data.QueryStreamItemsAsync<T>();
        }

        public IQueryable<T> Query<T>(IPerperStream stream)
        {
            var streamName = (stream as PerperFabricStream)!.StreamName;
            var data = _context.GetData(streamName);

            return data.QueryStreamItemsAsync<T>();
        }

        public Task<T> FetchStateAsync<T>()
        {
            var data = _context.GetData(StreamName);
            return data.FetchStreamParameterAsync<T>("context");
        }

        public async Task UpdateStateAsync<T>(T state)
        {
            var data = _context.GetData(StreamName);
            await data.UpdateStreamParameterAsync("context", state);
        }

        public IPerperStream GetStream()
        {
            var data = _context.GetData(StreamName);
            return data.GetStream();
        }

        public IPerperStream GetStream(string streamName)
        {
            var data = _context.GetData(streamName);
            return data.GetStream();
        }

        #region DeclareStream
        public IPerperStream DeclareStream(string streamName, string delegateName, Type? indexType = null)
        {
            var data = _context.GetData(StreamName);
            return data.DeclareStream(streamName, delegateName, indexType);
        }

        public IPerperStream DeclareStream(string name)
        {
            return DeclareStream(GenerateName(name), name);
        }

        public IPerperStream DeclareStream(string streamName, MethodInfo method, Type? indexType = null)
        {
            return DeclareStream(streamName, method.GetFullName(), indexType);
        }

        public IPerperStream DeclareStream(MethodInfo method)
        {
            return DeclareStream(method.GetFullName());
        }

        public IPerperStream DeclareStream(string streamName, Type type, Type? indexType = null)
        {
            return DeclareStream(streamName, type.GetFunctionMethod(), indexType);
        }

        public IPerperStream DeclareStream(Type type)
        {
            return DeclareStream(type.GetFunctionMethod());
        }
        #endregion

        #region StreamFunctionAsync
        public async Task<IPerperStream> StreamFunctionAsync(string streamName, string delegateName, object parameters, Type? indexType = null)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamFunctionAsync(streamName, delegateName, parameters, indexType);
        }

        public async Task<IPerperStream> StreamFunctionAsync(string name, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamFunctionAsync(GenerateName(name), name, parameters);
        }

        public Task<IPerperStream> StreamFunctionAsync(string streamName, MethodInfo method, object parameters, Type? indexType = null)
        {
            return StreamFunctionAsync(streamName, method.GetFullName(), parameters, indexType);
        }

        public Task<IPerperStream> StreamFunctionAsync(MethodInfo method, object parameters)
        {
            return StreamFunctionAsync(method.GetFullName(), parameters);
        }

        public Task<IPerperStream> StreamFunctionAsync(string streamName, Type type, object parameters, Type? indexType = null)
        {
            return StreamFunctionAsync(streamName, type.GetFunctionMethod(), parameters, indexType);
        }

        public Task<IPerperStream> StreamFunctionAsync(Type type, object parameters)
        {
            return StreamFunctionAsync(type.GetFunctionMethod(), parameters);
        }

        public async Task<IPerperStream> StreamFunctionAsync(IPerperStream declaration, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamFunctionAsync(declaration, parameters);
        }
        #endregion

        #region StreamActionAsync
        public async Task<IPerperStream> StreamActionAsync(string streamName, string delegateName, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamActionAsync(streamName, delegateName, parameters);
        }

        public async Task<IPerperStream> StreamActionAsync(string name, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamActionAsync(GenerateName(name),name, parameters);
        }

        public Task<IPerperStream> StreamActionAsync(string streamName, MethodInfo method, object parameters)
        {
            return StreamActionAsync(streamName, method.GetFullName(), parameters);
        }

        public Task<IPerperStream> StreamActionAsync(MethodInfo method, object parameters)
        {
            return StreamActionAsync(method.GetFullName(), parameters);
        }

        public Task<IPerperStream> StreamActionAsync(string streamName, Type type, object parameters)
        {
            return StreamActionAsync(streamName, type.GetFunctionMethod(), parameters);
        }

        public Task<IPerperStream> StreamActionAsync(Type type, object parameters)
        {
            return StreamActionAsync(type.GetFunctionMethod(), parameters);
        }

        public async Task<IPerperStream> StreamActionAsync(IPerperStream declaration, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamActionAsync(declaration, parameters);
        }
        #endregion

        #region CallWorkerAsync
        public async Task<T> CallWorkerAsync<T>(string name, object parameters, CancellationToken cancellationToken)
        {
            var data = _context.GetData(StreamName);
            var workerName = await data.CallWorkerAsync(GenerateName(name), name, DelegateName, parameters);
            var notifications = _context.GetNotifications(DelegateName);
            await foreach (var _ in notifications.WorkerResultSubmissions(StreamName, workerName, cancellationToken))
            {
                return await data.ReceiveWorkerResultAsync<T>(workerName);
            }

            throw new TimeoutException();
        }

        public Task<T> CallWorkerAsync<T>(MethodInfo method, object parameters, CancellationToken cancellationToken)
        {
            return CallWorkerAsync<T>(method.GetFullName(), parameters, cancellationToken);
        }

        public Task<T> CallWorkerAsync<T>(Type type, object parameters, CancellationToken cancellationToken)
        {
            return CallWorkerAsync<T>(type.GetFunctionMethod(), parameters, cancellationToken);
        }
        #endregion

        public Task BindOutput(CancellationToken cancellationToken)
        {
            return BindOutput(new IPerperStream[] { }, cancellationToken);
        }

        public Task BindOutput(IPerperStream stream, CancellationToken cancellationToken)
        {
            return BindOutput(new []{stream}, cancellationToken);
        }

        public async Task BindOutput(IEnumerable<IPerperStream> streams, CancellationToken cancellationToken)
        {
            await RebindOutput(streams);

            var tcs = new TaskCompletionSource<bool>();
            await using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }

        public async Task RebindOutput(IEnumerable<IPerperStream> streams)
        {
            var data = _context.GetData(StreamName);
            await data.BindStreamOutputAsync(streams);
        }

        private static string GenerateName(string delegateName)
        {
            return $"{delegateName.Replace("'", "").Replace(",", "")}-{Guid.NewGuid().ToString()}";
        }
    }
}