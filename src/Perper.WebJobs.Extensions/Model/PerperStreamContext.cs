using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public IAsyncDisposable GetStream()
        {
            var data = _context.GetData(StreamName);
            return data.GetStream();
        }

        public IAsyncDisposable DeclareStream(string name)
        {
            var data = _context.GetData(StreamName);
            return data.DeclareStream(name);
        }
        
        public async Task<IAsyncDisposable> StreamFunctionAsync(string name, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamFunctionAsync(name, parameters);
        }
        
        public async Task<IAsyncDisposable> StreamFunctionAsync(IAsyncDisposable declaration, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamFunctionAsync(declaration, parameters);
        }
        
        public async Task<IAsyncDisposable> StreamActionAsync(string name, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamActionAsync(name, parameters);
        }
        
        public async Task<IAsyncDisposable> StreamActionAsync(IAsyncDisposable declaration, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamActionAsync(declaration, parameters);
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

        public async Task<T> CallWorkerAsync<T>(string name, object parameters, CancellationToken cancellationToken)
        {
            var data = _context.GetData(StreamName);
            var workerName = await data.CallWorkerAsync(name, parameters);
            var notifications = _context.GetNotifications(DelegateName);
            await foreach (var _ in notifications.WorkerResultSubmissions(StreamName, workerName, cancellationToken))
            {
                return await data.ReceiveWorkerResultAsync<T>(workerName);
            }

            throw new TimeoutException();
        }

        public Task BindOutput(CancellationToken cancellationToken = default)
        {
            return BindOutput(new IAsyncDisposable[] { }, cancellationToken);
        }

        public Task BindOutput(IAsyncDisposable stream, CancellationToken cancellationToken = default)
        {
            return BindOutput(new []{stream}, cancellationToken);
        }

        public async Task BindOutput(IEnumerable<IAsyncDisposable> streams, CancellationToken cancellationToken = default)
        {
            var data = _context.GetData(StreamName);
            await data.BindStreamOutputAsync(streams);

            if (cancellationToken != default)
            {
                var tcs = new TaskCompletionSource<bool>();
                await using (cancellationToken.Register(s => ((TaskCompletionSource<bool>) s).TrySetResult(true), tcs))
                {
                    await tcs.Task.ConfigureAwait(false);
                }    
            }
        }
    }
}