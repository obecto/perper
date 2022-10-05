using System;

using Google.Protobuf;

using Perper.Protocol.Protobuf2;

namespace Perper.Protocol
{
    public class DefaultGrpc2Caster : IGrpc2Caster
    {
        public virtual IMessage SerializeValueToMessage(object value)
        {
            if (value is IMessage message)
            {
                return message;
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Override IGrpc2Caster to serialize value of type {value.GetType()} or use protobuf messages.");
            }
        }

        public virtual object DeserializeValueFromMessage(IMessage message, Type expectedType)
        {
            if (typeof(IMessage).IsAssignableFrom(expectedType))
            {
                return message;
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Override IGrpc2Caster to deserialize value of type {expectedType} or use protobuf messages.");
            }
        }
        /*
                object?[] PackArguments(ParameterInfo[]? parameters, object?[] arguments);
                object?[] UnpackArguments(ParameterInfo[]? parameters, object?[] packedArguments);

                object?[]? PackResult<TResult>(TResult result);
                TResult UnpackResult<TResult>(object?[]? packedResult);
        */
        public virtual Error SerializeException(Exception exception) => new() { Message = exception.Message };
#pragma warning disable CA2201 // TODO
        public virtual Exception DeserializeException(Error packedException) => new(packedException.Message);
#pragma warning restore CA2201
    }
}