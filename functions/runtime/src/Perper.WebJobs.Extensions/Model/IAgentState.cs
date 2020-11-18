using System;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IAgentState
    {
        Task<T> GetValue<T>(string key, Func<T> defaultValueFactory);
        Task SetValue<T>(string key, T value);

        IAgentState GetSubState(string name);
    }
}