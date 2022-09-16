
using Microsoft.Extensions.Hosting;

namespace Perper.Application.Listeners
{
    public interface IPerperListener : IHostedService
    {
        string Agent { get; }
    }
}