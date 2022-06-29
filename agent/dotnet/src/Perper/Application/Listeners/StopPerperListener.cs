using System;

using Perper.Application.Handlers;
using Perper.Model;

namespace Perper.Application.Listeners
{
    public class StopPerperListener : ExecutionPerperListener<VoidStruct>
    {
        public StopPerperListener(string agent, IPerperHandler<VoidStruct> handler, IServiceProvider services) : base(agent, PerperAgentsExtensions.StopFunctionName, handler, services)
        {
        }

        public static IPerperListener From(string agent, IPerperHandler handler, IServiceProvider services)
        {
            if (handler is IPerperHandler<VoidStruct> voidHandler)
            {
                return new StopPerperListener(agent, voidHandler, services);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Stop handler ({handler}) may not return a value.");
            }
        }
    }
}