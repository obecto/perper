using System.Text.RegularExpressions;

namespace Perper.Protocol.Cache
{
    public class WorkerBinaryTypeName
    {
        public string WorkerName { get; }
        public string DelegateName { get; }

        public WorkerBinaryTypeName(string workerName, string delegateName)
        {
            WorkerName = workerName;
            DelegateName = delegateName;
        }

        public override string ToString()
        {
            return $"{nameof(WorkerBinaryTypeName)}_{WorkerName}_{DelegateName}";
        }

        public static WorkerBinaryTypeName Parse(string stringValue)
        {
            var pattern = $@"{nameof(WorkerBinaryTypeName)}_(?'{nameof(WorkerName)}'.*)_(?'{nameof(DelegateName)}'.*)";
            var match = Regex.Match(stringValue, pattern);

            var workerName = match.Groups[nameof(WorkerName)].Value;
            var delegateName = match.Groups[nameof(DelegateName)].Value;

            return new WorkerBinaryTypeName(workerName, delegateName);
        }
    }
}