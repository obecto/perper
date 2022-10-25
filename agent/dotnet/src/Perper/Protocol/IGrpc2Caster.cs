using System;

using Google.Protobuf;

using Perper.Protocol.Protobuf2;

namespace Perper.Protocol
{
    public interface IGrpc2Caster
    {
        IMessage SerializeValueToMessage(object value);
        object DeserializeValueFromMessage(IMessage message, Type expectedType);

        Error SerializeException(Exception exception);
        Exception DeserializeException(Error packedException);

        object?[] PackArguments(ParameterInfo[]? parameters, object?[] arguments);
        object?[] UnpackArguments(ParameterInfo[]? parameters, object?[] packedArguments);

        object?[]? PackResult<TResult>(TResult result);
        TResult UnpackResult<TResult>(object?[]? packedResult);
    }
}