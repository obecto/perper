using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Perper.Application.Handlers;
using Perper.Model;
using Perper.Protocol;

#pragma warning disable CA1716

namespace Perper.Application.Listeners
{
    public class ExecutionPerperListener<TResult> : BackgroundService, IPerperListener
    {
        private readonly string Agent;
        private readonly string Delegate;
        private readonly IPerperHandler<TResult> Handler;
        private readonly IPerper Perper;
        private readonly PerperListenerFilter Filter;
        private readonly ILogger<ExecutionPerperListener<TResult>>? Logger;

        public ExecutionPerperListener(string agent, string @delegate, IPerperHandler<TResult> handler, IServiceProvider services)
        {
            Agent = agent;
            Delegate = @delegate;
            Handler = handler;
            Perper = services.GetRequiredService<IPerper>();
            Filter = services.GetRequiredService<PerperListenerFilter>();
            Logger = services.GetService<ILogger<ExecutionPerperListener<TResult>>>();
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash the whole service.")]
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Filter.Agent != null && Filter.Agent != Agent)
            {
                return Task.CompletedTask;
            }

            var taskCollection = new TaskCollection();

            taskCollection.AddRange(Perper.Executions.ListenAsync(new PerperExecutionFilter(Agent, Filter.Instance, Delegate), stoppingToken), async (executionData) =>
            {
                Logger?.LogDebug("Executing {Execution}", executionData.Execution);
                try
                {
                    var arguments = await Perper.Executions.GetArgumentsAsync(executionData.Execution, Handler.GetParameters()).ConfigureAwait(false);

                    var result = await Handler.Invoke(executionData, arguments).ConfigureAwait(false);

                    if (typeof(TResult) == typeof(VoidStruct))
                    {
                        await Perper.Executions.WriteResultAsync(executionData.Execution).ConfigureAwait(false);
                    }
                    else
                    {
                        await Perper.Executions.WriteResultAsync(executionData.Execution, result).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, "Exception while handling {Execution}", executionData);
                    try
                    {
                        await Perper.Executions.WriteExceptionAsync(executionData.Execution, e).ConfigureAwait(false);
                    }
                    catch (Exception e2)
                    {
                        Logger?.LogError(e2, "Exception while writing exception for {Execution}", executionData);
                    }
                }
            });

            return taskCollection.GetTask();
        }

        public override string ToString() => $"{GetType()}({Agent}, {Delegate}, {Handler})";
    }

    public static class ExecutionPerperListener
    {
        public static IPerperListener From(string agent, string @delegate, IPerperHandler handler, IServiceProvider services)
        {
            foreach (var type in handler.GetType().GetInterfaces())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPerperHandler<>))
                {
                    var resultType = type.GenericTypeArguments[0];
                    var listenerType = typeof(ExecutionPerperListener<>).MakeGenericType(resultType);
                    return (IPerperListener)Activator.CreateInstance(listenerType, agent, @delegate, handler, services)!;
                }
            }

            throw new ArgumentOutOfRangeException($"Execution handler ({handler}) must implement IPerperHandler<T>.");
        }
    }
}