using System.Text.RegularExpressions;

namespace Perper.Protocol.Header
{
    public class StateHeader
    {
        public string Name { get; }

        public StateHeader(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{nameof(StreamHeader)}<{Name}>";
        }

        public static StateHeader Parse(string stringValue)
        {
            var pattern = $@"{nameof(StreamHeader)}<(?'{nameof(Name)}'.*)>";
            var match = Regex.Match(stringValue, pattern);

            var name = match.Groups[nameof(Name)].Value;

            return new StateHeader(name);
        }
    }
}