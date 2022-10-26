using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Perper.Application.Handlers;
using Perper.Model;
using Perper.Protocol;

namespace Perper.Application.Listeners
{
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
    [SuppressMessage("Style", "CA1716:Using a reserved keyword as the name of a parameter on a virtual/interface member makes it harder for consumers in other languages to override/implement the member.", Justification = "We are using 'delegate' on purpose")]
    public abstract class LifecyclePerperListener : BackgroundService, IPerperListener
    {
        protected abstract string Delegate { get; }
        protected abstract PerperInstanceLifecycleState FromState { get; }
        protected abstract PerperInstanceLifecycleState ToState { get; }

        public string Agent { get; }
        private readonly IPerperHandler Handler;
        private readonly PerperListenerFilter Filter;
        private readonly PerperInstanceLifecycleService Lifecycle;
        private readonly ILogger<EnterContainerPerperListener>? Logger;

        protected LifecyclePerperListener(string agent, IPerperHandler handler, IServiceProvider services)
        {
            Agent = agent;
            Handler = handler;
            Filter = services.GetRequiredService<PerperListenerFilter>();
            Lifecycle = services.GetRequiredService<PerperInstanceLifecycleService>();
            Logger = services.GetService<ILogger<EnterContainerPerperListener>>();
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash the whole service.")]
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Filter.Agent != null && Filter.Agent != Agent)
            {
                return Task.CompletedTask;
            }

            var taskCollection = new TaskCollection();

            taskCollection.AddRange(Lifecycle.ListenWaitingForAsync(Agent, ToState, stoppingToken),
                async (instance) =>
                {
                    await Lifecycle.WaitForAsync(instance, FromState).ConfigureAwait(false);

                    var executionData = new PerperExecutionData(
                        instance,
                        Delegate,
                        new PerperExecution($"{instance.Instance}-{Delegate}"),
                        Array.Empty<object>(),
                        default)
                    {
                        IsSynthetic = true,
                    };

                    Logger?.LogDebug("Executing {Execution}", executionData);

                    try
                    {
                        var parameters = Handler.GetParameters() ?? Enumerable.Empty<ParameterInfo>();
                        var arguments = parameters.Select(param =>
                            param.HasDefaultValue ?
                                param.DefaultValue :
                                param.GetCustomAttribute<ParamArrayAttribute>() != null ?
                                    Array.CreateInstance(param.ParameterType.GetElementType()!, 0) :
                                    throw new ArgumentException($"Instance handler ({Handler}) may not have required arguments.")
                        ).ToArray();

                        await Handler.Invoke(executionData, arguments).ConfigureAwait(false);

                        Lifecycle.TransitionTo(instance, ToState);
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e, "Exception while handling {Execution}", executionData);
                    }
                });

            return taskCollection.GetTask();
        }

        public override string ToString() => $"{GetType()}({Agent}, {Handler})";
    }
}