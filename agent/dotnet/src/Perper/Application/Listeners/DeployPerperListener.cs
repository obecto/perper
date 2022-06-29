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
    public class DeployPerperListener : BackgroundService, IPerperListener
    {
        private readonly string Agent;
        private const string Delegate = "Deploy";
        private readonly IPerperHandler<VoidStruct> Handler;
        private readonly PerperListenerFilter Filter;
        private readonly ILogger<DeployPerperListener>? Logger;

        public DeployPerperListener(string agent, IPerperHandler<VoidStruct> handler, IServiceProvider services)
        {
            Agent = agent;
            Handler = handler;
            Filter = services.GetRequiredService<PerperListenerFilter>();
            Logger = services.GetService<ILogger<DeployPerperListener>>();
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash the whole service.")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Filter.Agent != null && Filter.Agent != Agent)
            {
                return;
            }

            var executionData = new PerperExecutionData(
                new PerperAgent("Deployer", Agent),
                Delegate,
                new PerperExecution($"{Agent}-deploy-deploy"),
                default);

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

        public static IPerperListener From(string agent, IPerperHandler handler, IServiceProvider services)
        {
            if (handler is IPerperHandler<VoidStruct> voidHandler)
            {
                return new DeployPerperListener(agent, voidHandler, services);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Deploy handler ({handler}) may not return a value.");
            }
        }
    }
}