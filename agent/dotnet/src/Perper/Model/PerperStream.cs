using System.Diagnostics.CodeAnalysis;

namespace Perper.Model
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "All that is *Stream is not made of Byte-s")]
    public class PerperStream
    {
        private readonly string stream;
        private readonly long startIndex;
        private readonly long stride;
        private readonly bool localToData;

        public PerperStream(
            string stream,
            long startIndex = -1,
            long stride = 0,
            bool localToData = false)
        {
            this.stream = stream;
            this.startIndex = startIndex;
            this.stride = stride;
            this.localToData = localToData;
        }

        public string Stream => stream;
        public long StartIndex => startIndex;
        public long Stride => stride;
        public bool LocalToData => localToData;

        public bool IsReplay => startIndex != -1;
        public bool IsPacked => stride != 0;

        public override string ToString() => $"PerperStream({Stream}, from: {StartIndex}, stride: {Stride}, local: {LocalToData})";
    }
}