using System;

using Perper.Application.Listeners;

namespace Perper.Application
{
    public interface IPerperBuilder
    {
        IPerperBuilder AddListener(Func<IServiceProvider, IPerperListener> listenerFactory);
    }
}