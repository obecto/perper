using System;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public static class AgentStateExtensions
    {
        public static Task<T> GetValue<T>(this IAgentState state, string key) where T : new()
        {
            return state.GetValue<T>(key, () => new T());
        }
    }
}