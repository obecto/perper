using System;
#pragma warning disable CA1716
namespace Perper.Application
{
    public interface IPerperBuilder
    {
        IPerperBuilder AddHandler(IPerperHandler handler);

        IPerperBuilder AddHandler(Func<IServiceProvider, IPerperHandler> handlerFactory);
    }
}