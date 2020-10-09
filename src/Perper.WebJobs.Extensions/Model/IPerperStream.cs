using System;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStream : IAsyncDisposable
    {
        IPerperStream Subscribe();
        IPerperStream Filter<T>(string fieldName, T value);
    }
}