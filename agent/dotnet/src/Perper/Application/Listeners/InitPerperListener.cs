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
    public class InitPerperListener : BackgroundService, IPerperListener
    {
        private readonly string Agent;
        private const string Delegate = "Init";
        private readonly IPerperHandler<VoidStruct> Handler;
        private readonly IPerper Perper;
        private readonly PerperListenerFilter Filter;
        private readonly ILogger<InitPerperListener>? Logger;

        public InitPerperListener(string agent, IPerperHandler<VoidStruct> handler, IServiceProvider services)
        {
            Agent = agent;
            Handler = handler;
            Perper = services.GetRequiredService<IPerper>();
            Filter = services.GetRequiredService<PerperListenerFilter>();
            Logger = services.GetService<ILogger<InitPerperListener>>();
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash the whole service.")]
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Filter.Agent != null && Filter.Agent != Agent)
            {
                return Task.CompletedTask;
            }

            var taskCollection = new TaskCollection();

            taskCollection.AddRange(
                Perper.Executions.ListenAsync(new PerperExecutionFilter("Registry", Agent, "Run"), stoppingToken)
                    .Where(x => Filter.Instance == null || Filter.Instance == x.Execution.Execution),
                async (rawExecutionData) =>
                {
                    var executionData = rawExecutionData with
                    {
                        Agent = new PerperAgent(Agent, rawExecutionData.Execution.Execution),
                        Delegate = Delegate,
                        Execution = new PerperExecution($"{rawExecutionData.Execution.Execution}-init")
                    };

                    try
                    {
                        var parameters = Handler.GetParameters() ?? Enumerable.Empty<ParameterInfo>();
                        var arguments = parameters.Select(param =>
                            param.HasDefaultValue ?
                                param.DefaultValue :
                                param.GetCustomAttribute<ParamArrayAttribute>() != null ?
                                    Array.CreateInstance(param.ParameterType.GetElementType()!, 0) :
                                    throw new ArgumentException($"Init handler ({Handler}) may not have required arguments.")
                        ).ToArray();

                        await Handler.Invoke(executionData, arguments).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e, "Exception while handling {Execution}", executionData);
                    }
                });

            return taskCollection.GetTask();
        }

        public override string ToString() => $"{GetType()}({Agent}, {Handler})";

        public static IPerperListener From(string agent, IPerperHandler handler, IServiceProvider services)
        {
            if (handler is IPerperHandler<VoidStruct> voidHandler)
            {
                return new InitPerperListener(agent, voidHandler, services);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Init handler ({handler}) may not return a value.");
            }
        }
    }
}