using System;

using Perper.Application.Handlers;
using Perper.Model;

namespace Perper.Application.Listeners
{
    public class StopPerperListener : ExecutionPerperListener
    {
        public StopPerperListener(string agent, IPerperHandler handler, IServiceProvider services) : base(agent, PerperAgentsExtensions.StopFunctionName, handler, services)
        {
        }
    }
}