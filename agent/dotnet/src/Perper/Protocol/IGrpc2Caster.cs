using System;
using System.Reflection;

using Google.Protobuf;

using Perper.Model;

namespace Perper.Protocol
{
    public interface IGrpc2Caster
    {
        IMessage SerializeValueToMessage(object? value);
        object? DeserializeValueFromMessage(IMessage message, Type expectedType);

        PerperError SerializeException(Exception exception);
        Exception DeserializeException(PerperError packedException);

        object?[] PackArguments(ParameterInfo[]? parameters, object?[] arguments);
        object?[] UnpackArguments(ParameterInfo[]? parameters, object?[] packedArguments);

        object?[]? PackResult<TResult>(TResult result);
        TResult UnpackResult<TResult>(object?[]? packedResult);
    }
}