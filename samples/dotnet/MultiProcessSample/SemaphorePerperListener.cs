﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Perper.Application.Listeners;
using Perper.Application.Handlers;
using Perper.Model;
using Perper.Protocol;

#pragma warning disable CA1716

namespace MultiProcessSample
{
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression")]
    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    public class SemaphorePerperListener : BackgroundService, IPerperListener
    {
        public string Agent { get; }
        public string Delegate { get; }
        private readonly IPerperHandler Handler;
        private readonly IPerper Perper;
        private readonly PerperListenerFilter Filter;
        private readonly ILogger<SemaphorePerperListener>? Logger;
        private readonly SemaphoreSlim ProcessSemaphore;

        public SemaphorePerperListener(int semaphoreCapacity, string agent, string @delegate, IPerperHandler handler, IServiceProvider services)
        {
            ProcessSemaphore = new(semaphoreCapacity);
            Agent = agent;
            Delegate = @delegate;
            Handler = handler;
            Perper = services.GetRequiredService<IPerper>();
            Filter = services.GetRequiredService<PerperListenerFilter>();
            Logger = services.GetService<ILogger<SemaphorePerperListener>>();
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash the whole service.")]
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Filter.Agent != null && Filter.Agent != Agent)
            {
                return Task.CompletedTask;
            }

            var taskCollection = new TaskCollection();

            taskCollection.Add(async () =>
            {
                await foreach (var executionData in Perper.Executions.ListenAsync(new PerperExecutionFilter(Agent, Filter.Instance, Delegate), stoppingToken))
                {
                    await ProcessSemaphore.WaitAsync();
                    taskCollection.Add(async () =>
                    {
                        Logger?.LogDebug("Executing {Execution}", executionData.Execution);
                        try
                        {
                            var arguments = await Perper.Executions.GetArgumentsAsync(executionData.Execution, Handler.GetParameters()).ConfigureAwait(false);

                            await Handler.Invoke(executionData, arguments).ConfigureAwait(false);
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
                        finally
                        {
                            ProcessSemaphore.Release();
                        }
                    });
                }
            });

            return taskCollection.GetTask();
        }

        public override string ToString() => $"{GetType()}({Agent}, {Delegate}, {Handler})";
    }
}