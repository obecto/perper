using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Model;

namespace Perper.Application.Handlers
{
    public class DoNothingPerperHandler : IPerperHandler
    {
        private readonly IPerper Perper;

        public DoNothingPerperHandler(IPerper perper) =>
            Perper = perper;

        public DoNothingPerperHandler(IServiceProvider services) : this(services.GetRequiredService<IPerper>()) { }

        public ParameterInfo[]? GetParameters() => null;

        public async Task Invoke(PerperExecutionData executionData, object?[] arguments)
        {
            if (!executionData.IsSynthetic)
            {
                await Perper.Executions.WriteResultAsync(executionData.Execution).ConfigureAwait(false);
            }
        }
    }
}