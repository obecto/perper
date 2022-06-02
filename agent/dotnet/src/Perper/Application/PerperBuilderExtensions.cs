using System;
using System.Globalization;
using System.Reflection;

namespace Perper.Application
{
    public static class PerperBuilderExtensions
    {
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
                // builder = type.Name == InitDelegateName ? builder.AddInitHandler(agent, type, runMethod) : builder.AddHandler(agent, type.Name, type, runMethod);
                builder = builder.AddHandler(agent, type.Name, type, runMethod);
            }

            return builder;
        }

        public static IPerperBuilder AddClassHandlers<T>(this IPerperBuilder builder) =>
            builder.AddClassHandlers(typeof(T));

        public static IPerperBuilder AddClassHandlers(this IPerperBuilder builder, Type type) =>
            builder.AddClassHandlers(type.Name, type);

        public static IPerperBuilder AddClassHandlers(this IPerperBuilder builder, string agent, Type type)
        {
            foreach (var method in type.GetMethods())
            {
                var name = method.Name;
                if (name.EndsWith(AsyncMethodSuffix, false, CultureInfo.InvariantCulture))
                {
                    name = name[..^AsyncMethodSuffix.Length];
                }
                // builder = name == InitDelegateName ? builder.AddInitHandler(agent, type, method) : builder.AddHandler(agent, name, type, method);
                builder = builder.AddHandler(agent, name, type, method);
            }

            return builder;
        }

        #region AddInitHandler
        public static IPerperBuilder AddInitHandler(this IPerperBuilder builder, string agent, Delegate handler) =>
            builder.AddHandler(agent, PerperHandlerService.InitFunctionName, handler);

        public static IPerperBuilder AddInitHandler<T>(this IPerperBuilder builder, string agent) =>
            builder.AddInitHandler(agent, typeof(T));

        public static IPerperBuilder AddInitHandler(this IPerperBuilder builder, string agent, Type initType) =>
            builder.AddHandler(agent, PerperHandlerService.InitFunctionName, initType, GetRunMethodOrThrow(initType));

        public static IPerperBuilder AddInitHandler(this IPerperBuilder builder, string agent, Type initType, MethodInfo method) =>
            builder.AddHandler(agent, PerperHandlerService.InitFunctionName, initType, method);

        public static IPerperBuilder AddInitHandler(this IPerperBuilder builder, string agent, Func<IServiceProvider, object?> createInstance, MethodInfo method) =>
            builder.AddHandler(agent, PerperHandlerService.InitFunctionName, createInstance, method);
        #endregion AddInitHandler

        #region AddHandler
        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, string @delegate, Delegate handler) =>
            builder.AddHandler(new MethodPerperHandler(agent, @delegate, handler));

        public static IPerperBuilder AddHandler<T>(this IPerperBuilder builder, string agent) =>
            builder.AddHandler(agent, typeof(T));

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, Type type) =>
            builder.AddHandler(new MethodPerperHandler(agent, type.Name, type, GetRunMethodOrThrow(type)));

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, string @delegate, Type type, MethodInfo method) =>
            builder.AddHandler(new MethodPerperHandler(agent, @delegate, type, method));

        public static IPerperBuilder AddHandler(this IPerperBuilder builder, string agent, string @delegate, Func<IServiceProvider, object?> createInstance, MethodInfo method) =>
            builder.AddHandler(new MethodPerperHandler(agent, @delegate, createInstance, method));
        #endregion AddHandler

        #region Utils
        private static MethodInfo? GetRunMethod(Type type)
        {
            return type.GetMethod(RunMethodName + AsyncMethodSuffix) ?? type.GetMethod(RunMethodName);
        }

        private static MethodInfo GetRunMethodOrThrow(Type type)
        {
            return GetRunMethod(type) ?? throw new ArgumentOutOfRangeException($"Type {type} does not contain a definition for {RunMethodName + AsyncMethodSuffix} or {RunMethodName}");
        }
        #endregion Utils
    }
}