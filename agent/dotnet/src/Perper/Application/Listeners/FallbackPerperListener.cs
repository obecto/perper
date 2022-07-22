using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Perper.Application.Handlers;

namespace Perper.Application.Listeners
{
    public class FallbackPerperListener : CompositePerperListener
    {
        private readonly string Agent;
        private readonly IServiceProvider Services;

        public FallbackPerperListener(string agent, IServiceProvider services)
        {
            Agent = agent;
            Services = services;
        }

        protected override IPerperListener[] GetListeners()
        {
            var hasStart = false;
            var hasStop = false;

            foreach (var service in Services.GetRequiredService<IEnumerable<IHostedService>>()) // HACK
            {
                if (service is StartPerperListener startListener && startListener.Agent == Agent)
                {
                    hasStart = true;
                }
                else if (service is StopPerperListener stopListener && stopListener.Agent == Agent)
                {
                    hasStop = true;
                }
            }

            var listeners = new List<IPerperListener>();

            if (!hasStart)
            {
                listeners.Add(new StartPerperListener(Agent, new DoNothingPerperHandler(Services), Services));
            }

            if (!hasStop)
            {
                listeners.Add(new StopPerperListener(Agent, new DoNothingPerperHandler(Services), Services));
            }

            return listeners.ToArray();
        }

        public override string ToString() => $"{GetType()}({Agent})";
    }
}