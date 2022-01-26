using System;
using System.Globalization;
using System.Reflection;

namespace Perper.Application
{
    public static class PerperStartupExtensions
    {
        public const string RunMethodName = "Run";
        public const string RunAsyncMethodName = "RunAsync";
        public const string StreamsNamespaceName = "Streams";
        public const string CallsNamespaceName = "Calls";
        public const string InitDelegateName = "Init";

        public static PerperStartup AddAssemblyHandlers(this PerperStartup startup, string? agent = null, Assembly? assembly = null, string? rootNamespace = null)
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
                startup = type.Name == InitDelegateName ? startup.AddInitHandler(agent, type, runMethod) : startup.AddHandler(agent, type.Name, type, runMethod);
            }

            return startup;
        }

        public static PerperStartup AddClassHandlers<T>(this PerperStartup startup) =>
            startup.AddClassHandlers(typeof(T));

        public static PerperStartup AddClassHandlers(this PerperStartup startup, Type type) =>
            startup.AddClassHandlers(type.Name, type);

        public static PerperStartup AddClassHandlers(this PerperStartup startup, string agent, Type type)
        {
            foreach (var method in type.GetMethods())
            {
                var name = method.Name;
                if (name.EndsWith("Async", false, CultureInfo.InvariantCulture))
                {
                    name = name[..^"Async".Length];
                }
                startup = name == InitDelegateName ? startup.AddInitHandler(agent, type, method) : startup.AddHandler(agent, name, type, method);
            }

            return startup;
        }

        #region AddInitHandler
        public static PerperStartup AddInitHandler(this PerperStartup startup, string agent, Delegate handler) =>
            startup.AddInitHandler(agent, PerperStartupHandlerUtils.CreateHandler(handler, isInit: true));

        public static PerperStartup AddInitHandler<T>(this PerperStartup startup, string agent) =>
            startup.AddInitHandler(agent, typeof(T));

        public static PerperStartup AddInitHandler(this PerperStartup startup, string agent, Type initType) =>
            startup.AddInitHandler(agent, initType, GetRunMethodOrThrow(initType));

        public static PerperStartup AddInitHandler(this PerperStartup startup, string agent, Type initType, MethodInfo method) =>
            startup.AddInitHandler(agent, () => CreateInstance(initType), method);

        public static PerperStartup AddInitHandler(this PerperStartup startup, string agent, Func<object> createInstance, MethodInfo method) =>
            startup.AddInitHandler(agent, PerperStartupHandlerUtils.CreateHandler(createInstance, method, isInit: true));
        #endregion AddInitHandler

        #region AddHandler
        public static PerperStartup AddHandler(this PerperStartup startup, string agent, string @delegate, Delegate handler) =>
            startup.AddHandler(agent, @delegate, PerperStartupHandlerUtils.CreateHandler(handler, isInit: false));

        public static PerperStartup AddHandler<T>(this PerperStartup startup, string agent) =>
            startup.AddHandler(agent, typeof(T));

        public static PerperStartup AddHandler(this PerperStartup startup, string agent, Type type) =>
            startup.AddHandler(agent, type.Name, type, GetRunMethodOrThrow(type));

        public static PerperStartup AddHandler(this PerperStartup startup, string agent, string @delegate, Type type, MethodInfo method) =>
            startup.AddHandler(agent, @delegate, () => CreateInstance(type), method);

        public static PerperStartup AddHandler(this PerperStartup startup, string agent, string @delegate, Func<object> createInstance, MethodInfo method) =>
            startup.AddHandler(agent, @delegate, PerperStartupHandlerUtils.CreateHandler(createInstance, method, isInit: false));
        #endregion AddHandler

        #region Utils
        private static MethodInfo? GetRunMethod(Type type)
        {
            return type.GetMethod(RunAsyncMethodName) ?? type.GetMethod(RunMethodName);
        }

        private static MethodInfo GetRunMethodOrThrow(Type type)
        {
            return GetRunMethod(type) ?? throw new ArgumentOutOfRangeException($"Type {type} does not contain a definition for {RunAsyncMethodName} or {RunMethodName}");
        }

        private static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type)!;
        }
        #endregion Utils
    }
}