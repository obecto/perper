using System;

using Perper.Application.Handlers;
using Perper.Model;

#pragma warning disable CA1716

namespace Perper.Application.Listeners
{
    public class StreamPerperListener : CompositePerperListener
    {
        private readonly string Agent;
        private readonly string Delegate;
        private readonly PerperStreamOptions StreamOptions;
        private readonly IPerperHandler Handler;
        private readonly IServiceProvider Services;

        protected virtual string ExternalDelegate => Delegate;
        protected virtual string InternalDelegate => $"{Delegate}-stream";

        public StreamPerperListener(string agent, string @delegate, PerperStreamOptions streamOptions, IPerperHandler handler, IServiceProvider services)
        {
            Agent = agent;
            Delegate = @delegate;
            StreamOptions = streamOptions;
            Handler = handler;
            Services = services;
        }

        protected override IPerperListener[] GetListeners()
        {
            return new IPerperListener[]
            {
                new ExecutionPerperListener(Agent, ExternalDelegate, new StartStreamPerperHandler(StreamOptions, InternalDelegate, Services), Services),
                new ExecutionPerperListener(Agent, InternalDelegate, Handler, Services)
            };
        }

        public override string ToString() => $"{GetType()}({Agent}, {ExternalDelegate}, {InternalDelegate}, {StreamOptions}, {Handler})";
    }
}