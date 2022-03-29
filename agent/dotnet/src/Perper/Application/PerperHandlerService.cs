using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Perper.Model;
using Perper.Protocol;

namespace Perper.Application
{
    public class PerperHandlerService : BackgroundService
    {
        public static string InitFunctionName { get; } = "Init";

        private readonly IEnumerable<IPerperHandler> Handlers;
        private readonly IPerper Perper;
        private readonly IServiceProvider ServiceProvider;
        private readonly PerperConfiguration PerperConfiguration;

        public PerperHandlerService(IEnumerable<IPerperHandler> handlers, IPerper perper, IServiceProvider serviceProvider, IOptions<PerperConfiguration> perperOptions)
        {
            Handlers = handlers;
            Perper = perper;
            ServiceProvider = serviceProvider;
            PerperConfiguration = perperOptions.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var taskCollection = new TaskCollection();

            var agentHasStartup = new Dictionary<string, bool>();

            foreach (var handler in Handlers)
            {
                if (PerperConfiguration.Agent != null && PerperConfiguration.Agent != handler.Agent)
                {
                    continue;
                }

                if (handler.Delegate == InitFunctionName)
                {
                    var initInstance = $"{handler.Agent}-init";
                    if (PerperConfiguration.Instance != null && PerperConfiguration.Instance != initInstance)
                    {
                        continue;
                    }

                    var initExecution = new PerperExecutionData(new PerperAgent(handler.Agent, initInstance), "Init", new PerperExecution($"{initInstance}-init"), stoppingToken);
                    taskCollection.Add(async () =>
                    {
                        //await using (ServiceProvider.CreateAsyncScope()) // TODO: #if NET6_0 ?
                        using var scope = ServiceProvider.CreateScope();
                        scope.ServiceProvider.GetRequiredService<PerperScopeService>().SetExecution(initExecution);
                        Console.WriteLine($"hi! {handler.Agent} {handler.Delegate}");
                        await handler.Handle(scope.ServiceProvider).ConfigureAwait(false);
                    });
                }
                else
                {
                    if (handler.Delegate == PerperAgentsExtensions.StartupFunctionName)
                    {
                        agentHasStartup[handler.Agent] = true;
                    }
                    else
                    {
                        agentHasStartup.TryAdd(handler.Agent, false);
                    }

                    ListenExecutions(taskCollection, handler, stoppingToken);
                }
            }

            foreach (var (agent, hasStartup) in agentHasStartup)
            {
                if (hasStartup)
                {
                    continue;
                }

                ListenExecutions(taskCollection, new EmptyPerperHandler(agent, PerperAgentsExtensions.StartupFunctionName), stoppingToken);
            }

            return taskCollection.GetTask();
        }

        private void ListenExecutions(TaskCollection taskCollection, IPerperHandler handler, CancellationToken stoppingToken)
        {
            taskCollection.Add(async () =>
            {
                var filter = new PerperExecutionFilter(handler.Agent, PerperConfiguration.Instance, handler.Delegate);
                await foreach (var execution in Perper.Executions.ListenAsync(filter, stoppingToken))
                {
                    taskCollection.Add(async () =>
                    {
                        try
                        {
                            //await using (ServiceProvider.CreateAsyncScope()) // TODO: #if NET6_0 ?
                            using var scope = ServiceProvider.CreateScope();
                            scope.ServiceProvider.GetRequiredService<PerperScopeService>().SetExecution(execution);
                            await handler.Handle(scope.ServiceProvider).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    });
                }
            });
        }
    }
}