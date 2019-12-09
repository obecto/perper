using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Header
{
    public class StreamHeader
    {
        public string Name { get; set; }
        public StreamKind Kind { get; set; }

        public StreamHeader(string name, StreamKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public StreamHeader(string stringValue)
        {
            var pattern = $@"{nameof(StreamHeader)}<(?'{nameof(Name)}'.*),(?'{nameof(Kind)}'.*)>";
            var match = Regex.Match(stringValue, pattern);

            Name = match.Groups[nameof(Name)].Value;
            Kind = Enum.Parse<StreamKind>(match.Groups[nameof(Kind)].Value);
        }

        public override string ToString()
        {
            return $"{nameof(StreamHeader)}<{Name},{Kind}>";
        }
    }

    public enum StreamKind
    {
        Pipe,
        Sink
    }
}