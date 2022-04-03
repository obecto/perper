using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Perper.Application.Handlers;
using Perper.Model;
using Perper.Protocol;

namespace Perper.Application
{
    public class PerperHandlerService : BackgroundService
    {
        public static string InitFunctionName { get; } = "Init";

        private readonly IEnumerable<IPerperHandler> _handlers;
        private readonly IPerper _perper;
        private readonly IServiceProvider _serviceProvider;
        private readonly PerperConfiguration _perperConfiguration;

        public PerperHandlerService(IEnumerable<IPerperHandler> handlers, IPerper perper, IServiceProvider serviceProvider, IOptions<PerperConfiguration> perperOptions)
        {
            _handlers = handlers;
            _perper = perper;
            _serviceProvider = serviceProvider;
            _perperConfiguration = perperOptions.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var taskCollection = new TaskCollection();

            var agentHasStartup = new Dictionary<string, bool>();

            foreach (var handler in _handlers)
            {
                if (_perperConfiguration.Agent != null && _perperConfiguration.Agent != handler.Agent)
                {
                    continue;
                }

                if (handler.Delegate == InitFunctionName)
                {
                    var initInstance = $"{handler.Agent}-init";
                    if (_perperConfiguration.Instance != null && _perperConfiguration.Instance != initInstance)
                    {
                        continue;
                    }

                    var initExecution = new PerperExecutionData(new PerperAgent(handler.Agent, initInstance), "Init", new PerperExecution($"{initInstance}-init"), stoppingToken);
                    taskCollection.Add(async () =>
                    {
                        //await using (ServiceProvider.CreateAsyncScope()) // TODO: #if NET6_0 ?
                        using var scope = _serviceProvider.CreateScope();
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

        private void ListenExecutions(TaskCollection taskCollection, IPerperHandler handler, CancellationToken stoppingToken) =>
            taskCollection.Add(async () =>
            {
                var filter = new PerperExecutionFilter(handler.Agent, _perperConfiguration.Instance, handler.Delegate);
                await foreach (var execution in _perper.Executions.ListenAsync(filter, stoppingToken))
                {
                    taskCollection.Add(async () =>
                    {
                        try
                        {
                            //await using (ServiceProvider.CreateAsyncScope()) // TODO: #if NET6_0 ?
                            using var scope = _serviceProvider.CreateScope();
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