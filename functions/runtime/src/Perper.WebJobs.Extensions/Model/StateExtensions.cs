using System;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public static class StateExtensions
    {
        public static Task<T> GetValue<T>(this IState state, string key) where T : new() => state.GetValue(key, () => new T());

        public static Task<IStateEntry<T>> Entry<T>(this IState state, string key) where T : new() => state.Entry(key, () => new T());
    }
}