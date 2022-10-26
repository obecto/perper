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
    public class RunInstancePerperListener : BackgroundService, IPerperListener
    {
        public static string RunInstanceDelegate => "RunInstance";

        public string Agent { get; }
        private readonly IPerperHandler Handler;
        private readonly IPerper Perper;
        private readonly PerperListenerFilter Filter;
        private readonly ILogger<RunInstancePerperListener>? Logger;

        public RunInstancePerperListener(string agent, IPerperHandler handler, IServiceProvider services)
        {
            Agent = agent;
            Handler = handler;
            Perper = services.GetRequiredService<IPerper>();
            Filter = services.GetRequiredService<PerperListenerFilter>();
            Logger = services.GetService<ILogger<RunInstancePerperListener>>();
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
                        Agent = new PerperInstance(Agent, rawExecutionData.Execution.Execution),
                        Delegate = RunInstanceDelegate,
                        Execution = new PerperExecution($"{rawExecutionData.Execution.Execution}-{RunInstanceDelegate}"),
                        IsSynthetic = true
                    };

                    try
                    {
                        await Invoke(executionData).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e, "Exception while handling {Execution}", executionData);
                    }
                });

            return taskCollection.GetTask();
        }

        protected virtual async Task Invoke(PerperExecutionData executionData)
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
        }

        public override string ToString() => $"{GetType()}({Agent}, {Handler})";
    }
}