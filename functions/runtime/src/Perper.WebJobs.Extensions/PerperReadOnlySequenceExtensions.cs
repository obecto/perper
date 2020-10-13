using System;
using System.Buffers;
using System.Text;

namespace Perper.WebJobs.Extensions
{
    public static class PerperReadonlySequenceExtensions
    {
        public static bool TryReadLengthDelimitedMessage(this ReadOnlySequence<byte> buffer, out ushort messageLength)
        {
            messageLength = default;
            if (buffer.Length < sizeof(ushort)) return false;

            messageLength = BitConverter.ToUInt16(buffer.Slice(0, sizeof(ushort)).ToArray());
            return buffer.Length >= messageLength + sizeof(ushort);
        }
    }
}