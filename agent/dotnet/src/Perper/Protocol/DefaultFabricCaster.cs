using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Configuration;

namespace Perper.Protocol
{
    public class DefaultFabricCaster : IFabricCaster
    {
        public object?[] PackArguments(ParameterInfo[]? parameters, object?[] arguments)
        {
            return arguments;
        }

        public object?[] UnpackArguments(ParameterInfo[]? parameters, object?[] packedArguments)
        {
            if (parameters == null)
            {
                return packedArguments;
            }

            var arguments = new object?[parameters.Length];
            for (var i = 0 ; i < parameters.Length ; i++)
            {
                try
                {
                    object? argument;
                    if (i == parameters.Length - 1 && parameters[i].GetCustomAttribute<ParamArrayAttribute>() != null)
                    {
                        var paramsType = parameters[i].ParameterType.GetElementType()!;
                        if (i < packedArguments.Length)
                        {
                            var paramsArray = Array.CreateInstance(paramsType, packedArguments.Length - i);
                            for (var j = 0 ; j < paramsArray.Length ; j++)
                            {
                                paramsArray.SetValue(UnpackArgument(paramsType, packedArguments[i + j]), j);
                            }
                            argument = paramsArray;
                        }
                        else
                        {
                            argument = Array.CreateInstance(paramsType, 0);
                        }
                    }
                    else if (i < packedArguments.Length)
                    {
                        argument = UnpackArgument(parameters[i].ParameterType, packedArguments[i]);
                    }
                    else
                    {
                        if (!parameters[i].HasDefaultValue)
                        {
                            throw new ArgumentException($"Not enough arguments passed; expected at least {i + 1}, got {packedArguments.Length}");
                        }
                        argument = parameters[i].DefaultValue;
                    }
                    arguments[i] = argument;
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Failed decoding parameter {i + 1} ({parameters[i]})", e);
                }
            }

            return arguments;
        }

        public static object? UnpackArgument(Type parameterType, object? arg)
        {
            return arg != null && parameterType.IsAssignableFrom(arg.GetType())
                ? arg
                : arg is ArrayList arrayList && parameterType == typeof(object[])
                    ? arrayList.Cast<object>().ToArray()
                    : Convert.ChangeType(arg, parameterType, CultureInfo.InvariantCulture);
        }


        public object?[]? PackResult<TResult>(TResult result)
        {
            if (result == null)
            {
                return null;
            }
            else
            {
                object?[] packedResult;

                if (result is ITuple tuple)
                {
                    packedResult = new object?[tuple.Length];
                    for (var i = 0 ; i < packedResult.Length ; i++)
                    {
                        packedResult[i] = tuple[i];
                    }
                }
                else
                {
                    packedResult = result is object?[] _results && typeof(TResult) == typeof(object[]) ? _results : (new object?[] { result });
                }

                return packedResult;
            }
        }

        public TResult UnpackResult<TResult>(object?[]? packedResult)
        {
            if (packedResult is null)
            {
                return default!;
            }
            else if (typeof(ITuple).IsAssignableFrom(typeof(TResult)))
            {
                return (TResult)Activator.CreateInstance(typeof(TResult), packedResult)!;
            }
            else if (typeof(object[]) == typeof(TResult))
            {
                return (TResult)(object)packedResult;
            }
            else if (packedResult.Length >= 1)
            {
                return (TResult)packedResult[0]!;
            }
            else
            {
                return default!;
            }
        }

        public string PackException(Exception exception)
        {
            return exception.Message;
        }

        public Exception UnpackException(string packedException)
        {
            return new InvalidOperationException($"Execution failed with error: {packedException}"); // TODO: Fix exception type
        }

        public object PackItem<TItem>(TItem item)
        {
            return item!;
        }

        public TItem UnpackItem<TItem>(object packedItem)
        {
            return (TItem)packedItem!;
        }

        public IEnumerable<QueryEntity> TypeToQueryEntities(Type type)
        {
            var queryEntity = new QueryEntity(type);
            if (queryEntity.Fields == null)
            {
                Console.WriteLine($"Warning: Stream indexing requested for type {queryEntity.ValueTypeName}, but no fields are configured; this can cause problems when writing items. Consider annotating one or more properties of {type} with [Apache.Ignite.Core.Cache.Configuration.QuerySqlField].");
            }
            yield return queryEntity;
        }

        public bool TypeShouldKeepBinary(Type type)
        {
            return type == typeof(IBinaryObject);
        }
    }
}