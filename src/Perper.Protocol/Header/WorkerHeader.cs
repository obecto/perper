using System.Text.RegularExpressions;

namespace Perper.Protocol.Header
{
    public class WorkerHeader
    {
        public bool IsResult { get; }

        public WorkerHeader(bool isResult)
        {
            IsResult = isResult;
        }
        
        public override string ToString()
        {
            return $"{nameof(WorkerHeader)}<{IsResult}>";
        }

        public static WorkerHeader Parse(string stringValue)
        {
            var pattern = $@"{nameof(StreamHeader)}<(?'{nameof(IsResult)}'.*)>";
            var match = Regex.Match(stringValue, pattern);

            var isResult = bool.Parse(match.Groups[nameof(IsResult)].Value);

            return new WorkerHeader(isResult);
        }
    }
}