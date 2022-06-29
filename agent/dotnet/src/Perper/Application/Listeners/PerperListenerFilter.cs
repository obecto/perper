
using Microsoft.Extensions.Options;

namespace Perper.Application.Listeners
{
    public record PerperListenerFilter(string? Agent, string? Instance)
    {
        public PerperListenerFilter(IOptions<PerperConfiguration> options) : this(options.Value.Agent, options.Value.Instance) { }
    }
}