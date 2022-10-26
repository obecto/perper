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
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression")]
    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    public class ExecutionPerperListener : BackgroundService, IPerperListener
    {
        public string Agent { get; }
        public string Delegate { get; }
        private readonly IPerperHandler Handler;
        private readonly IPerper Perper;
        private readonly PerperListenerFilter Filter;
        private readonly PerperInstanceLifecycleService Lifecycle;
        private readonly ILogger<ExecutionPerperListener>? Logger;

        public ExecutionPerperListener(string agent, string @delegate, IPerperHandler handler, IServiceProvider services)
        {
            Agent = agent;
            Delegate = @delegate;
            Handler = handler;
            Perper = services.GetRequiredService<IPerper>();
            Filter = services.GetRequiredService<PerperListenerFilter>();
            Lifecycle = services.GetRequiredService<PerperInstanceLifecycleService>();
            Logger = services.GetService<ILogger<ExecutionPerperListener>>();
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash the whole service.")]
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Filter.Agent != null && Filter.Agent != Agent)
            {
                return Task.CompletedTask;
            }

            var taskCollection = new TaskCollection();

            taskCollection.AddRange(Perper.Executions.ListenAsync(new PerperExecutionFilter(Agent, Filter.Instance, Delegate) { Parameters = Handler.GetParameters() }, stoppingToken), async (executionData) =>
            {
                await Lifecycle.WaitForAsync(executionData.Agent, PerperInstanceLifecycleState.EnteredContainer).ConfigureAwait(false);
                Logger?.LogDebug("Executing {Execution}", executionData.Execution);
                try
                {
                    await Handler.Invoke(executionData, executionData.Arguments).ConfigureAwait(false);
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
}