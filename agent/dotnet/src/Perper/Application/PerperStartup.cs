using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Protocol;

namespace Perper.Application
{
    public class PerperStartup
    {
        public bool UseInstances { get; set; } = false;
        public bool UseDeployInit { get; set; } = false;
        private readonly List<(string, Func<Task>)> initHandlers = new();
        private readonly Dictionary<string, Dictionary<string, Func<Task>>> executionHandlers = new();

        public PerperStartup WithDeployInit()
        {
            UseDeployInit = true;
            return this;
        }

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
            await using (fabricService.ConfigureAwait(false))
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

                if (UseDeployInit)
                {
                    var initInstance = instance ?? $"{agent}-init";
                    var initExecution = new FabricExecution(agent, initInstance, "init", $"{initInstance}-init",
                        cancellationToken);

                    taskCollection.Add(async () =>
                    {
                        AsyncLocals.SetExecution(initExecution);
                        await handler().ConfigureAwait(false);
                    });
                }
                else
                {
                    ListenExecutions(taskCollection, agent, instance, "Init", handler, true, cancellationToken);
                }
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

                // Note: Deprecated - exists just for backwards compatibility with old `Startup`. Will be removed soon.
                // Projects should switch to using `Start` instead.
                agentExecutionHandlers.TryAdd(PerperContext.FallbackStartupFunctionName, async () =>
                {
                    await AsyncLocals.FabricService.WriteExecutionFinished(AsyncLocals.Execution).ConfigureAwait(false);
                });

                agentExecutionHandlers.TryAdd(PerperContext.StopFunctionName, async () =>
                {
                    await AsyncLocals.FabricService.WriteExecutionFinished(AsyncLocals.Execution).ConfigureAwait(false);
                });

                foreach (var (@delegate, handler) in agentExecutionHandlers)
                {
                    ListenExecutions(taskCollection, agent, instance, @delegate, handler, false, cancellationToken);
                }
            }

            return taskCollection.GetTask();
        }

        public static void ListenExecutions(TaskCollection taskCollection, string agent, string? instance,
            string @delegate, Func<Task> handler, bool isInit, CancellationToken cancellationToken)
        {
            taskCollection.Add(async () =>
            {
                var executions = isInit
                    ? AsyncLocals.FabricService.GetExecutionsReader("Registry", agent, "Run")
                        .ReadAllAsync(cancellationToken)
                        .Where(x => instance == null || instance == x.Execution)
                        .Select(x => x with
                        {
                            Agent = agent,
                            Instance = x.Execution,
                            Delegate = @delegate,
                            Execution = $"{x.Execution}-init"
                        })
                    : AsyncLocals.FabricService.GetExecutionsReader(agent, instance, @delegate)
                        .ReadAllAsync(cancellationToken);

                await foreach (var execution in executions.WithCancellation(cancellationToken))
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