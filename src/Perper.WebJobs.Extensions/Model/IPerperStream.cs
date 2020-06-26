using System;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStream : IAsyncDisposable
    {
        IPerperStream GetRef();
    }
}