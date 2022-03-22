using System;
using System.Threading.Tasks;

#pragma warning disable CA1716

namespace Perper.Application
{
    public interface IPerperHandler
    {
        string Agent { get; }

        string Delegate { get; }

        Task Handle(IServiceProvider serviceProvider);
    }
}