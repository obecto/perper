using System;
using System.Collections.Generic;
using System.Reflection;

using Apache.Ignite.Core.Cache.Configuration;

namespace Perper.Protocol
{
    public interface IFabricCaster
    {
        object?[] PackArguments(ParameterInfo[]? parameters, object?[] arguments);
        object?[] UnpackArguments(ParameterInfo[]? parameters, object?[] packedArguments);

        object?[]? PackResult<TResult>(TResult result);
        TResult UnpackResult<TResult>(object?[]? packedResult);

        string PackException(Exception exception);
        Exception UnpackException(string packedException);

        object PackItem<TItem>(TItem item);
        TItem UnpackItem<TItem>(object packedItem);

        IEnumerable<QueryEntity> TypeToQueryEntities(Type type);
        bool TypeShouldKeepBinary(Type type);
    }
}