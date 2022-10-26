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

namespace Perper.Application.Listeners
{
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
    public class DeployPerperListener : BackgroundService, IPerperListener
    {
        public string Agent { get; }
        private const string Delegate = "Deploy";
        private readonly IPerperHandler Handler;
        private readonly PerperListenerFilter Filter;
        private readonly PerperInstanceLifecycleService Lifecycle;
        private readonly ILogger<DeployPerperListener>? Logger;

        public DeployPerperListener(string agent, IPerperHandler handler, IServiceProvider services)
        {
            Agent = agent;
            Handler = handler;
            Filter = services.GetRequiredService<PerperListenerFilter>();
            Lifecycle = services.GetRequiredService<PerperInstanceLifecycleService>();
            Logger = services.GetService<ILogger<DeployPerperListener>>();
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash the whole service.")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Filter.Agent != null && Filter.Agent != Agent)
            {
                return;
            }

            // NOTE: PerperInstance("Deployer", Agent) would be more "correct", but it breaks the simple usecase of using Deploy to make calls in the same agent.
            var executionData = new PerperExecutionData(
                new PerperInstance(Agent, $"{Agent}-deploy"),
                Delegate,
                new PerperExecution($"{Agent}-deploy-deploy"),
                Array.Empty<object>(),
                default)
            {
                IsSynthetic = true,
            };

            Lifecycle.TransitionTo(executionData.Agent, PerperInstanceLifecycleState.EnteredContainer); // HACK

            try
            {
                var parameters = Handler.GetParameters() ?? Enumerable.Empty<ParameterInfo>();
                var arguments = parameters.Select(param =>
                    param.HasDefaultValue ?
                        param.DefaultValue :
                        param.GetCustomAttribute<ParamArrayAttribute>() != null ?
                            Array.CreateInstance(param.ParameterType.GetElementType()!, 0) :
                            throw new ArgumentException($"Deploy Handler ({Handler}) may not have required arguments.")
                ).ToArray();

                await Handler.Invoke(executionData, arguments).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Exception while deploying {Execution}", executionData);
            }
        }

        public override string ToString() => $"{GetType()}({Agent}, {Handler})";
    }
}