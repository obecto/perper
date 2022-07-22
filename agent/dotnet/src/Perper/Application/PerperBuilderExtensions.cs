using System;
using System.Globalization;
using System.Reflection;

using Perper.Application.Handlers;
using Perper.Application.Listeners;
using Perper.Model;

namespace Perper.Application
{
    public static class PerperBuilderExtensions
    {
        public const string DeployDelegateName = "Deploy";
        public const string InitDelegateName = "Init";
        public const string StartDelegateName = "Start";
        public const string FallbackStartDelegateName = "Startup";
        public const string StopDelegateName = "Stop";
        public const string RunMethodName = "Run";
        public const string AsyncMethodSuffix = "Async";

        public static IPerperBuilder AddAssemblyHandlers(this IPerperBuilder builder, string? agent = null, Assembly? assembly = null, string? rootNamespace = null)
        {
            assembly ??= Assembly.GetEntryAssembly()!;
            agent ??= assembly.GetName().Name!;
            rootNamespace ??= assembly.GetName().Name!;

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass
                    || type.MemberType != MemberTypes.TypeInfo
                    || string.IsNullOrEmpty(type.Namespace)
                    || !type.Namespace!.StartsWith(rootNamespace, false, CultureInfo.InvariantCulture))
                {
                    continue;
                }

                var runMethod = GetRunMethod(type);
                if (runMethod is null)
                {
                    continue;
                }

                builder = builder.AddHandler(agent, type.Name, type, runMethod);
            }

            builder = builder.AddFallbackHandlers(agent);

            return builder;
        }

        public static IPerperBuilder AddClassHandlers<T>(this IPerperBuilder builder) =>
            builder.AddClassHandlers(typeof(T));

        public static IPerperBuilder AddClassHandlers(this IPerperBuilder builder, Type type) =>
            builder.AddClassHandlers(type.Name, type);

        public static IPerperBuilder AddDeploySingletonHandler(this IPerperBuilder builder, string agent) =>
            builder.AddListener(services => new DeployPerperListener(agent, new DeploySingletonPerperHandler(services), services));

        public static IPerperBuilder AddClassHandlers(this IPerperBuilder builder, string agent, Type type)
        {
            foreach (var method in type.GetMethods())
            {
                var name = method.Name;
                if (name.EndsWith(AsyncMethodSuffix, false, CultureInfo.InvariantCulture))
                {
                    name = name[..^AsyncMethodSuffix.Length];
                }

                builder = builder.AddHandler(agent, name, type, method);
            }

            builder = builder.AddFallbackHandlers(agent);

            return builder;
        }

        public static IPerperBuilder AddHandler<T>(this IPerperBuilder builder, string agent) =>
            builder.AddHandler(agent, typeof(T));

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, Type type) =>
            builder.AddHandler(agent, type.Name, type, GetRunMethodOrThrow(type));

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, string @delegate, Type type) =>
            builder.AddHandler(agent, @delegate, type, GetRunMethodOrThrow(type));

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, Type type, MethodInfo method) =>
            builder.AddHandler(agent, StripSuffix(method.Name, AsyncMethodSuffix), type, GetRunMethodOrThrow(type));

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, string @delegate, Type type, MethodInfo method) =>
            builder.AddHandler(agent, @delegate, services => new MethodPerperHandler(type, method, services), method.GetCustomAttribute<PerperStreamOptionsAttribute>()?.Options);

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, string @delegate, Delegate handler) =>
            builder.AddHandler(agent, @delegate, services => new MethodPerperHandler(handler, services), handler.Method.GetCustomAttribute<PerperStreamOptionsAttribute>()?.Options);

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, string @delegate, Func<IServiceProvider, IPerperHandler> handlerFactory, PerperStreamOptions? streamOptions = null) =>
            builder.AddListener(services =>
            {
                var handler = handlerFactory(services);

                if (streamOptions != null || (handler is MethodPerperHandler methodHandler && methodHandler.IsStream))
                {
                    return new StreamPerperListener(agent, @delegate, streamOptions ?? new PerperStreamOptions(), handler, services);
                }
                else if (@delegate == DeployDelegateName)
                {
                    return new DeployPerperListener(agent, handler, services);
                }
                else if (@delegate == InitDelegateName)
                {
                    return new InitPerperListener(agent, handler, services);
                }
                else if (@delegate == StartDelegateName || @delegate == FallbackStartDelegateName)
                {
                    return new StartPerperListener(agent, handler, services);
                }
                else if (@delegate == StopDelegateName)
                {
                    return new StopPerperListener(agent, handler, services);
                }
                else
                {
                    return new ExecutionPerperListener(agent, @delegate, handler, services);
                }
            });

        public static IPerperBuilder AddFallbackHandlers(this IPerperBuilder builder, string agent) => builder.AddListener(services => new FallbackPerperListener(agent, services));


        private static string StripSuffix(string @string, string suffix) =>
            @string.EndsWith(suffix, false, CultureInfo.InvariantCulture) ? @string[..^suffix.Length] : @string;

        private static MethodInfo? GetRunMethod(Type type)
        {
            return type.GetMethod(RunMethodName + AsyncMethodSuffix) ?? type.GetMethod(RunMethodName);
        }

        private static MethodInfo GetRunMethodOrThrow(Type type)
        {
            return GetRunMethod(type) ?? throw new ArgumentOutOfRangeException($"Type {type} does not contain a definition for {RunMethodName + AsyncMethodSuffix} or {RunMethodName}");
        }
    }
}