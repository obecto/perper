using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Perper.Application.Handlers;

namespace Perper.Application.Listeners
{
    public class FallbackPerperListener : CompositePerperListener
    {
        public override string Agent { get; }
        private readonly IServiceProvider Services;

        public FallbackPerperListener(string agent, IServiceProvider services)
        {
            Agent = agent;
            Services = services;
        }

        protected override IPerperListener[] GetListeners()
        {
            var typesFound = new HashSet<Type>();

            foreach (var service in Services.GetRequiredService<IEnumerable<IHostedService>>()) // HACK
            {
                if (service is IPerperListener listener && listener.Agent == Agent)
                {
                    typesFound.Add(listener.GetType());
                }
            }

            var listeners = new List<IPerperListener>();

            if (!typesFound.Contains(typeof(UpgradePerperListener)))
            {
                listeners.Add(new UpgradePerperListener(Agent, new DoNothingPerperHandler(Services), Services));
            }

            if (!typesFound.Contains(typeof(EnterContainerPerperListener)))
            {
                listeners.Add(new EnterContainerPerperListener(Agent, new DoNothingPerperHandler(Services), Services));
            }

            if (!typesFound.Contains(typeof(StartPerperListener)))
            {
                listeners.Add(new StartPerperListener(Agent, new DoNothingPerperHandler(Services), Services));
            }

            if (!typesFound.Contains(typeof(StopPerperListener)))
            {
                listeners.Add(new StopPerperListener(Agent, new DoNothingPerperHandler(Services), Services));
            }

            return listeners.ToArray();
        }

        public override string ToString() => $"{GetType()}({Agent})";
    }
}