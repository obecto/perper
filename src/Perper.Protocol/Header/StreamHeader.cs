using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Header
{
    public class StreamHeader
    {
        public string Name { get; }
        public StreamKind Kind { get; }

        public StreamHeader(string name, StreamKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public override string ToString()
        {
            return $"{nameof(StreamHeader)}<{Name},{Kind}>";
        }

        public static StreamHeader Parse(string stringValue)
        {
            var pattern = $@"{nameof(StreamHeader)}<(?'{nameof(Name)}'.*),(?'{nameof(Kind)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            
            var name = match.Groups[nameof(Name)].Value; 
            var kind = Enum.Parse<StreamKind>(match.Groups[nameof(Kind)].Value);
            
            return new StreamHeader(name, kind);
        }
    }

    public enum StreamKind
    {
        Pipe,
        Sink
    }
}