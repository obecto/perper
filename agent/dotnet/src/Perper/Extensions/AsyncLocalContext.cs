using System;
using System.Threading;

using Perper.Model;

namespace Perper.Extensions
{
    internal sealed class AsyncLocalContext : IDisposable
    {
        private static readonly AsyncLocal<IPerperContext> AsyncLocal = new();
        public static IPerperContext PerperContext => AsyncLocal.Value!;

        private readonly IPerperContext _oldValue;
        public AsyncLocalContext(IPerperContext newValue)
        {
            _oldValue = AsyncLocal.Value!;
            AsyncLocal.Value = newValue;
        }

        public void Dispose()
        {
            AsyncLocal.Value = _oldValue!;
            GC.SuppressFinalize(this);
        }
    }
}