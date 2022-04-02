using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Protocol;

namespace Perper.Application
{
    public class PerperStartup
    {
        public bool UseInstances { get; set; } = false;
        private readonly List<(string, Func<Task>)> initHandlers = new();
        private readonly Dictionary<string, Dictionary<string, Func<Task>>> executionHandlers = new();

        public PerperStartup AddInitHandler(string agent, Func<Task> handler)
        {
            initHandlers.Add((agent, handler));
            return this;
        }

        public PerperStartup AddHandler(string agent, string @delegate, Func<Task> handler)
        {
            if (!executionHandlers.TryGetValue(agent, out var agentExecutionHandlers))
            {
                executionHandlers[agent] = agentExecutionHandlers = new();
            }
            agentExecutionHandlers.Add(@delegate, handler);
            return this;
        }

        public PerperStartup WithInstances()
        {
            UseInstances = true;
            return this;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var fabricService = await PerperConnection.EstablishConnection().ConfigureAwait(false);
            await using(fabricService.ConfigureAwait(false))
            {
                AsyncLocals.SetConnection(fabricService);

                await RunInServiceContext(cancellationToken).ConfigureAwait(false); 
            }            
        }

        public Task RunInServiceContext(CancellationToken cancellationToken = default)
        {
            var (instanceAgent, instance) = UseInstances ? PerperConnection.ConfigureInstance() : (null, null);

            var taskCollection = new TaskCollection();

            foreach (var (agent, handler) in initHandlers)
            {
                if (instanceAgent != null && instanceAgent != agent)
                {
                    continue;
                }

                var initInstance = instance ?? $"{agent}-init";
                var initExecution = new FabricExecution(agent, initInstance, "Init", $"{initInstance}-init", cancellationToken);
                taskCollection.Add(async () =>
                {
                    AsyncLocals.SetExecution(initExecution);
                    await handler().ConfigureAwait(false);
                });
            }

            foreach (var (agent, agentExecutionHandlers) in executionHandlers)
            {
                if (instanceAgent != null && instanceAgent != agent)
                {
                    continue;
                }

                agentExecutionHandlers.TryAdd(PerperContext.StartupFunctionName, async () =>
                {
                    await AsyncLocals.FabricService.WriteExecutionFinished(AsyncLocals.Execution).ConfigureAwait(false);
                });

                foreach (var (@delegate, handler) in agentExecutionHandlers)
                {
                    ListenExecutions(taskCollection, agent, instance, @delegate, handler, cancellationToken);
                }
            }

            return taskCollection.GetTask();
        }

        public static void ListenExecutions(TaskCollection taskCollection, string agent, string? instance, string @delegate, Func<Task> handler, CancellationToken cancellationToken)
        {
            taskCollection.Add(async () =>
            {
                await foreach (var execution in AsyncLocals.FabricService.GetExecutionsReader(agent, instance, @delegate).ReadAllAsync(cancellationToken))
                {
                    taskCollection.Add(async () =>
                    {
                        AsyncLocals.SetExecution(execution);
                        await handler().ConfigureAwait(false);
                    });
                }
            });
        }

        [Obsolete("Use `new PerperStartup().AddAssemblyHandlers().RunAsync()` instead")]
        public static Task RunAsync(string agent, CancellationToken cancellationToken = default)
        {
            return new PerperStartup().AddAssemblyHandlers(agent).RunAsync(cancellationToken);
        }
    }
}