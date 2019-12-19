using System;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class PerperStreamContext : IPerperStreamContext
    {
        private readonly IPerperFabricContext _context;
        private readonly string _streamName;
        private readonly string _name;

        public PerperStreamContext(IPerperFabricContext context, string streamName, string name)
        {
            _context = context;
            _streamName = streamName;
            _name = name;
        }

        public async Task<IAsyncDisposable> StreamActionAsync(string name, object parameters)
        {
            var data = _context.GetData(_streamName);
            return await data.StreamActionAsync(name, parameters);
        }

        public async Task<IAsyncDisposable> StreamFunctionAsync(string name, object parameters)
        {
            var data = _context.GetData(_streamName);
            return await data.StreamFunctionAsync(name, parameters);
        }

        public Task<T> FetchStateAsync<T>()
        {
            var data = _context.GetData(_streamName);
            return data.FetchStreamParameter<T>(_name);
        }

        public async Task UpdateStateAsync<T>(T state)
        {
            var data = _context.GetData(_streamName);
            await data.UpdateStreamParameterAsync(_name, state);
        }

        public async Task<T> CallWorkerAsync<T>(object parameters)
        {
            var data = _context.GetData(_streamName);
            await data.InvokeWorkerAsync(parameters);
            var notifications = _context.GetNotifications(_streamName);
            await foreach (var _ in notifications.WorkerResultSubmissions())
            {
                return await data.ReceiveWorkerResultAsync<T>();
            }

            throw new TimeoutException();
        }
    }
}