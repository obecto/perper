using System;

using Perper.Application.Handlers;
using Perper.Model;

namespace Perper.Application.Listeners
{
    public class StartPerperListener : ExecutionPerperListener
    {
        public StartPerperListener(string agent, IPerperHandler handler, IServiceProvider services)
            : base(agent, PerperAgentsExtensions.StartFunctionName, handler, services)
        {
        }
    }
}