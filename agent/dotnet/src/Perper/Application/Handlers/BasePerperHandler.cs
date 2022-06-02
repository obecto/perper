using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Extensions;
using Perper.Protocol;

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

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        public async Task Handle(IServiceProvider serviceProvider) // TODO: Handle Init() out of class
        {
            var fabricService = serviceProvider.GetRequiredService<FabricService>();
            var fabricExecution = serviceProvider.GetRequiredService<FabricExecution>();
            if (Delegate != PerperHandlerService.InitFunctionName)
            {
                try
                {
                    var arguments = await fabricService.ReadExecutionParameters(fabricExecution.Execution).ConfigureAwait(false);

                    var result = await Handle(serviceProvider, arguments).ConfigureAwait(false);

                    await fabricService.WriteExecutionResult(fabricExecution.Execution, result).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception while executing {fabricExecution.Execution}: {e}");
                    try
                    {
                        await fabricService.WriteExecutionException(fabricExecution.Execution, e).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine($"Exception while executing {fabricExecution.Execution}: {e2}");
                    }
                }
            }
            else
            {
                try
                {
                    await Handle(serviceProvider, Array.Empty<object?>()).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception while executing {fabricExecution.Execution}: {e}");
                }
            }
        }

        protected abstract Task<object?[]> Handle(IServiceProvider serviceProvider, object?[] arguments);
    }
}