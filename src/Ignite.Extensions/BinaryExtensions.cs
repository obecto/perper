using System;
using Apache.Ignite.Core.Binary;

namespace Ignite.Extensions
{
    public static class BinaryExtensions
    {
        public static IBinaryObject GetBinaryObjectFromBytes(this IBinary binary, byte[] value)
        {
            throw new NotImplementedException();
        }
        
        public static byte[] GetBytesFromBinaryObject(this IBinary binary, IBinaryObject value)
        {
            throw new NotImplementedException();
        }
    }
}