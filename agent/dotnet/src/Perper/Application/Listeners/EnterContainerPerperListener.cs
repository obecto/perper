using System;

using Perper.Application.Handlers;

namespace Perper.Application.Listeners
{
    public class EnterContainerPerperListener : LifecyclePerperListener
    {
        public static string EnterContainerDelegate => "EnterContainer";

        protected override string Delegate => EnterContainerDelegate;
        protected override PerperInstanceLifecycleState FromState => PerperInstanceLifecycleState.Upgraded;
        protected override PerperInstanceLifecycleState ToState => PerperInstanceLifecycleState.EnteredContainer;

        public EnterContainerPerperListener(string agent, IPerperHandler handler, IServiceProvider services)
            : base(agent, handler, services)
        {
        }
    }
}