using System;
using Apache.Ignite.Core.Binary;

namespace Ignite.Extensions
{
    public static class BinaryExtensions
    {
        public static IBinaryObject GetBinaryObjectFromBytes(this IBinary binary, byte[] value)
        {
            //TODO: message size delimited and change to return type to IEnumerable
            //TODO: handle incomplete bytes chunks
            throw new NotImplementedException();
        }
        
        public static byte[] GetBytesFromBinaryObject(this IBinary binary, IBinaryObject value)
        {
            //TODO: message size delimited and change the input type to IEnumerable
            //TODO: handle incomplete bytes chunks
            throw new NotImplementedException();
        }

        public static IBinaryObjectBuilder GetCacheObjectBuilder(this IBinary binary, string cacheName)
        {
            
            return binary.GetBuilder($"Ignite.Extensions.Cache.{cacheName}");
        }
        
        public static IBinaryObjectBuilder GetCacheObjectBuilder(this IBinary binary, string cacheName,
            Type cacheType)
        {
            
            return binary.GetBuilder($"Ignite.Extensions.Cache.{cacheName}<{cacheType}>");
        }

        public static bool TryParseCacheObjectTypeName(this IBinaryObject binaryObject, out string cacheName, out Type cacheType)
        {
            throw new NotImplementedException();
        }
    }
}