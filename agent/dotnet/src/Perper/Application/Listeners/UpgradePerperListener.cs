using System;

using Perper.Application.Handlers;

namespace Perper.Application.Listeners
{
    public class UpgradePerperListener : LifecyclePerperListener
    {
        public static string UpgradeDelegate => "Upgrade";

        protected override string Delegate => UpgradeDelegate;
        protected override PerperInstanceLifecycleState FromState => PerperInstanceLifecycleState.Uninitialized;
        protected override PerperInstanceLifecycleState ToState => PerperInstanceLifecycleState.Upgraded;

        public UpgradePerperListener(string agent, IPerperHandler handler, IServiceProvider services)
            : base(agent, handler, services)
        {
        }
    }
}