using System;
using System.Threading;

using Perper.Model;

namespace Perper.Extensions
{
    internal sealed class AsyncLocalContext : IDisposable
    {
        private static readonly AsyncLocal<IPerperContext> AsyncLocal = new();
        public static IPerperContext PerperContext => AsyncLocal.Value!;

        private readonly IPerperContext OldValue;
        public AsyncLocalContext(IPerperContext newValue)
        {
            OldValue = AsyncLocal.Value!;
            AsyncLocal.Value = newValue;
        }

        public void Dispose()
        {
            AsyncLocal.Value = OldValue!;
            GC.SuppressFinalize(this);
        }
    }
}