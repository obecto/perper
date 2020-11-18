using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;

namespace Perper.WebJobs.Extensions.Model
{
    public class StreamStateManager
    {
        [NonSerialized] private List<IStreamState> states = new List<IStreamState>();

        public void AttachStreamState(IStreamState state)
        {
            states.Add(state);
        }

        public Task LoadStreamStates()
        {
            return Task.WhenAll(states.Select(x => x.Load()));
        }

        public Task StoreStreamStates()
        {
            return Task.WhenAll(states.Select(x => x.Store()));
        }
    }
}