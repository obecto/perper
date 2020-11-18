using System;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public class StreamState<T> : IStreamState<T> where T : new()
    {
        [NonSerialized] private string _streamName;
        [NonSerialized] private IAgentState _agentState;

        public T Value { get; set; }

        public async Task Load()
        {
            Value = await _agentState.GetValue<T>(_streamName);
        }

        public Task Store()
        {
            return _agentState.SetValue<T>(_streamName, Value);
        }
    }
}