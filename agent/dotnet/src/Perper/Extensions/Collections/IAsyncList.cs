using System.Collections.Generic;
using System.Threading.Tasks;

namespace Perper.Extensions.Collections
{
    public interface IAsyncList<T> : IAsyncEnumerable<T>
    {
        public string Name { get; }

        public Task<int> CountAsync();

        public Task AddAsync(T item);

        public Task ClearAsync();

        public Task<bool> ContainsAsync(T item);

        public Task<int> IndexOfAsync(T item);

        public Task InsertAsync(int index, T item);

        public Task<bool> RemoveAsync(T item);

        public Task RemoveAtAsync(int index);

        public Task<T> PopAsync();

        public Task<T> DequeueAsync();   
    }
}
