using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

using Google.Protobuf;

using Microsoft.Extensions.Options;

using Perper.Model;

using WellKnownTypes = Google.Protobuf.WellKnownTypes;

namespace Perper.Protocol
{
    public class DefaultGrpc2Caster : IGrpc2Caster
    {
        protected FabricConfiguration Configuration { get; }

        public DefaultGrpc2Caster(IOptions<FabricConfiguration> configuration) =>
            Configuration = configuration.Value;

        public virtual IMessage SerializeValueToMessage(object? value)
        {
            if (value is IMessage message)
            {
                return message;
            }
            else if (value is null)
            {
                return WellKnownTypes.Value.ForNull();
            }
            else if (value is int i)
            {
                return new WellKnownTypes.Int32Value() { Value = i };
            }
            else if (value is string s)
            {
                return new WellKnownTypes.StringValue() { Value = s };
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Override IGrpc2Caster to serialize value of type {value.GetType()} or use protobuf messages.");
            }
        }

        public virtual object? DeserializeValueFromMessage(IMessage message, Type expectedType)
        {
            if (message is WellKnownTypes.Value)
            {
                return null; // FIXME
            }
            if (message is WellKnownTypes.Int32Value iv)
            {
                return iv.Value;
            }
            if (message is WellKnownTypes.StringValue sv)
            {
                return sv.Value;
            }
            if (expectedType.IsAssignableFrom(message.GetType()))
            {
                return message;
            }
            throw new ArgumentOutOfRangeException($"Override IGrpc2Caster to deserialize value of type {message.GetType()} / {expectedType} or use protobuf messages.");
        }

        public virtual PerperError SerializeException(Exception exception) => new() { Message = exception.Message };
#pragma warning disable CA2201 // TODO
        public virtual Exception DeserializeException(PerperError packedException) => new(packedException.Message);
#pragma warning restore CA2201

        public object?[] PackArguments(ParameterInfo[]? parameters, object?[] arguments) => arguments;

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

        private static object? UnpackArgument(Type parameterType, object? arg) =>
            arg != null && parameterType.IsInstanceOfType(arg)
                ? arg
                : Convert.ChangeType(arg, parameterType, CultureInfo.InvariantCulture);

        public object?[]? PackResult<TResult>(TResult result)
        {
            if (result == null)
            {
                return null;
            }

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
                packedResult = result is object?[] results && typeof(TResult) == typeof(object[]) ? results : (new object?[] { result });
            }

            return packedResult;
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

        public CacheOptions GetCacheOptions(PerperStream stream, PerperStreamOptions options)
        {
            return ConvertPersistenceOptions(options.PersistenceOptions) ??
                new CacheOptions()
                {
                    DataRegion = options.Persistent ? Configuration.PersistentStreamDataRegion : Configuration.EphemeralStreamDataRegion
                };
        }

        public CacheOptions GetCacheOptions(PerperDictionary dictionary, PerperStateOptions? options)
        {
            return ConvertPersistenceOptions(options?.PersistenceOptions) ??
                new CacheOptions()
                {
                    DataRegion = Configuration.StateDataRegion
                };
        }

        public CacheOptions GetCacheOptions(PerperList list, PerperStateOptions? options)
        {
            return ConvertPersistenceOptions(options?.PersistenceOptions) ??
                new CacheOptions()
                {
                    DataRegion = Configuration.StateDataRegion
                };
        }

        protected virtual CacheOptions? ConvertPersistenceOptions(object? persistenceOptions)
        {
            return persistenceOptions switch
            {
                //CacheClientConfiguration configuration => new CacheClientConfiguration(configuration),
                CacheOptions options => new CacheOptions(options),
                string dataRegion => new CacheOptions() { DataRegion = dataRegion },
                _ => null,
            };
        }
    }
}