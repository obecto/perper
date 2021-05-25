using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Apache.Ignite.Core.Binary;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperNameMapper : IBinaryNameMapper
    {
        private readonly ILogger _logger;

        private readonly Dictionary<string, Type> _fullNameMap = new Dictionary<string, Type>();
        public Dictionary<Type, string> WellKnownTypes { get; } = new Dictionary<Type, string>();

        public PerperNameMapper(ILogger<PerperNameMapper> logger)
        {
            _logger = logger;
        }

        public void InitializeFromAppDomain()
        {
            var seenWellKnownTypeNames = new Dictionary<string, Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    _fullNameMap[type.FullName ?? "-"] = type;

                    var wellKnownName = GetWellKnownTypeName(type);
                    if (wellKnownName != null)
                    {
                        _logger.LogDebug($"Found PerperData type '{wellKnownName}' as {type.FullName}");
                        WellKnownTypes[type] = wellKnownName;

                        if (seenWellKnownTypeNames.TryGetValue(wellKnownName, out var existingType))
                        {
                            _logger.LogError($"Naming collision between {type.AssemblyQualifiedName} and {existingType.AssemblyQualifiedName} (both named '{wellKnownName}', try using PerperData(Name = ...) to change one of them)");
                        }
                        seenWellKnownTypeNames[wellKnownName] = type;
                    }
                }
            }
        }

        private string? GetWellKnownTypeName(Type type)
        {
            var attribute = type.GetCustomAttribute<PerperDataAttribute>();
            if (attribute != null)
            {
                if (attribute.Name != null)
                {
                    if (type.IsGenericType)
                    {
                        return $"{attribute.Name}`{type.GetGenericArguments().Length}";
                    }
                    else
                    {
                        return attribute.Name;
                    }
                }

                return type.Name;
            }
            return null;
        }

        public string GetTypeName(string name)
        {
            var type = Type.GetType(name);
            if (type == null && !_fullNameMap.TryGetValue(name, out type))
            {
                return name;
            }

            return GetTypeName(type);
        }

        public string GetTypeName(Type type)
        {
            if (WellKnownTypes.TryGetValue(type, out var result))
            {
                return result;
            }
            else if (type.IsArray)
            {
                var baseType = type.GetElementType()!;
                return $"{GetTypeName(baseType)}[{new string(',', type.GetArrayRank() - 1)}]";
            }
            else if (type.IsConstructedGenericType)
            {
                var baseType = type.GetGenericTypeDefinition();
                Debug.Assert(baseType != type); // Recursion guard

                var builder = new StringBuilder(GetTypeName(baseType));

                builder.Append('[');
                var isFirst = true;

                foreach (var argument in type.GetGenericArguments())
                {
                    if (!isFirst)
                    {
                        builder.Append(',');
                    }
                    isFirst = false;

                    builder.Append('[');
                    builder.Append(GetTypeName(argument));
                    builder.Append(']');
                }

                builder.Append(']');

                return builder.ToString();
            }
            else
            {
                return type.FullName ?? type.GUID.ToString();
            }
        }

        public string GetFieldName(string name)
        {
            return name;
        }
    }
}