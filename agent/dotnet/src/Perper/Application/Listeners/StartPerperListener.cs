using System;

using Perper.Application.Handlers;
using Perper.Model;

namespace Perper.Application.Listeners
{
    public class StartPerperListener<TResult> : ExecutionPerperListener<TResult>
    {
        public StartPerperListener(string agent, IPerperHandler<TResult> handler, IServiceProvider services) : base(agent, PerperAgentsExtensions.StartFunctionName, handler, services)
        {
        }
    }

    public static class StartPerperListener
    {
        public static IPerperListener From(string agent, IPerperHandler handler, IServiceProvider services)
        {
            foreach (var type in handler.GetType().GetInterfaces())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPerperHandler<>))
                {
                    var resultType = type.GenericTypeArguments[0];
                    var listenerType = typeof(StartPerperListener<>).MakeGenericType(resultType);
                    return (IPerperListener)Activator.CreateInstance(listenerType, agent, handler, services)!;
                }
            }

            throw new ArgumentOutOfRangeException($"Start handler ({handler}) must implement IPerperHandler<T>.");
        }
    }
}