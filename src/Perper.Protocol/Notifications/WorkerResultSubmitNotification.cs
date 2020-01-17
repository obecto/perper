using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class WorkerResultSubmitNotification
    {
        public string StreamName { get; }
        public string WorkerName { get; }

        public WorkerResultSubmitNotification(string streamName, string workerName)
        {
            StreamName = streamName;
            WorkerName = workerName;
        }

        public override string ToString()
        {
            return $"{nameof(WorkerResultSubmitNotification)}<{StreamName}%{WorkerName}>";
        }

        public static WorkerResultSubmitNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(WorkerResultSubmitNotification))) throw new ArgumentException();
            
            var pattern = $@"{nameof(WorkerResultSubmitNotification)}<(?'{nameof(StreamName)}'.*)%(?'{nameof(WorkerName)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var streamName = match.Groups[nameof(StreamName)].Value;
            var workerName = match.Groups[nameof(WorkerName)].Value;
            
            return new WorkerResultSubmitNotification(streamName, workerName);
        }
    }
}