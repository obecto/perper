using System;
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

        public async Task<IAsyncDisposable> StreamActionAsync(string name, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamActionAsync(name, parameters);
        }

        public async Task<IAsyncDisposable> StreamFunctionAsync(string name, object parameters)
        {
            var data = _context.GetData(StreamName);
            return await data.StreamFunctionAsync(name, parameters);
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

        public async Task<T> CallWorkerAsync<T>(object parameters, CancellationToken cancellationToken)
        {
            var data = _context.GetData(StreamName);
            await data.InvokeWorkerAsync(parameters);
            var notifications = _context.GetNotifications(DelegateName);
            await foreach (var _ in notifications.WorkerResultSubmissions(cancellationToken))
            {
                return await data.ReceiveWorkerResultAsync<T>();
            }

            throw new TimeoutException();
        }

        public async Task WaitUntilCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            await using (cancellationToken.Register(s => ((TaskCompletionSource<bool>) s).TrySetResult(true), tcs))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}