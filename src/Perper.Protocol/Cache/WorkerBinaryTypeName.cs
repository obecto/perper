using System;

namespace Perper.Protocol.Cache
{
    public class WorkerBinaryTypeName
    {
        public override string ToString()
        {
            return $"{nameof(WorkerBinaryTypeName)}";
        }

        public static WorkerBinaryTypeName Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(WorkerBinaryTypeName))) throw new ArgumentException();
            return new WorkerBinaryTypeName();
        }
    }
}