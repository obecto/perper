using System;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IState
    {
        Task<T> GetValue<T>(string key, Func<T> defaultValueFactory);

        Task SetValue<T>(string key, T value);

        Task<IStateEntry<T>> Entry<T>(string key, Func<T> defaultValueFactory);
    }
}