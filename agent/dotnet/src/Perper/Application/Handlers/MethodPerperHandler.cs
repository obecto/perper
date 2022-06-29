using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Extensions;
using Perper.Model;

namespace Perper.Application.Handlers
{
    public class MethodPerperHandler<TResult> : IPerperHandler<TResult>
    {
        private readonly Func<IServiceProvider, object?>? CreateInstance;
        private readonly MethodInfo Method;
        private readonly IServiceProvider Services;

        public MethodPerperHandler(Func<IServiceProvider, object?>? createInstance, MethodInfo method, IServiceProvider services)
        {
            CreateInstance = method.IsStatic ? null : createInstance;
            Method = method;
            Services = services;
        }

        public ParameterInfo[]? GetParameters() => Method.GetParameters();

        public async Task<TResult> Invoke(PerperExecutionData executionData, object?[] arguments)
        {
            //await using (Services.CreateAsyncScope()) // TODO: #if NET6_0 ?
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<PerperScopeService>().SetExecution(executionData);

            using (scope.ServiceProvider.GetRequiredService<IPerperContext>().UseContext())
            {
                var instance = CreateInstance?.Invoke(scope.ServiceProvider);
                var result = Method.Invoke(instance, arguments);

                // HACK: ! Currently MethodPerperHandler takes care of ValueTask/Task results, since otherwise the context and scope are destroyed too early.
                if (typeof(TResult) == typeof(VoidStruct))
                {
                    if (result is ValueTask valueTask)
                    {
                        await valueTask.ConfigureAwait(false);
                    }
                    else if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                    }
                    return (TResult)(object)VoidStruct.Value;
                }
                else
                {
                    if (result is ValueTask<TResult> valueTask)
                    {
                        result = await valueTask.ConfigureAwait(false);
                    }
                    else if (result is Task<TResult> task)
                    {
                        result = await task.ConfigureAwait(false);
                    }
                    return (TResult)result!;
                }
            }
        }
    }

    public static class MethodPerperHandler
    {
        public static IPerperHandler From(Func<IServiceProvider, object?>? createInstance, MethodInfo method, IServiceProvider services)
        {
            var returnType = method.ReturnType;

            var resultType = returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(ValueTask)
                ? typeof(VoidStruct)
                : returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                    ? returnType.GenericTypeArguments[0]
                    : returnType;
            var handlerType = typeof(MethodPerperHandler<>).MakeGenericType(resultType);
            return (IPerperHandler)Activator.CreateInstance(handlerType, createInstance, method, services)!;
        }

        public static IPerperHandler From(Type type, MethodInfo method, IServiceProvider services) =>
            From((serviceProvider) => ActivatorUtilities.CreateInstance(serviceProvider, type), method, services);

        public static IPerperHandler From(Delegate handler, IServiceProvider services) =>
            From((_) => handler.Target!, handler.Method, services);
    }
}