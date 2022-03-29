using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Perper.Model;

namespace Perper.Application
{
    public abstract class BasePerperHandler : IPerperHandler
    {
        public string Agent { get; }

        public string Delegate { get; }

        protected BasePerperHandler(string agent, string @delegate)
        {
            Agent = agent;
            Delegate = @delegate;
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash the whole application.")]
        public async Task Handle(IServiceProvider serviceProvider) // TODO: Handle Init() out of class
        {
            var logger = serviceProvider.GetService<ILogger<BasePerperHandler>>();
            var perperContext = serviceProvider.GetRequiredService<IPerperContext>();
            try
            {
                if (Delegate != PerperHandlerService.InitFunctionName)
                {
                    try
                    {
                        var arguments = await perperContext.Executions.GetArgumentsAsync(perperContext.CurrentExecution, GetParameters()).ConfigureAwait(false);

                        var (resultType, result) = await Handle(serviceProvider, arguments).ConfigureAwait(false);

                        await perperContext.Executions.WriteResultAsync(perperContext.CurrentExecution, resultType, result).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, $"Exception while executing {perperContext.CurrentExecution.Execution}");
                        await perperContext.Executions.WriteExceptionAsync(perperContext.CurrentExecution, e).ConfigureAwait(false);
                    }
                }
                else
                {
                    await Handle(serviceProvider, Array.Empty<object?>()).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"Exception while executing {perperContext.CurrentExecution.Execution}");
            }
        }

        protected virtual ParameterInfo[]? GetParameters() => null;

        protected abstract Task<(Type, object?)> Handle(IServiceProvider serviceProvider, object?[] arguments);
    }
}