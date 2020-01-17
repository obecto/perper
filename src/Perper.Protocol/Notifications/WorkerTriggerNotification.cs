using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class WorkerTriggerNotification
    {
        public string StreamName { get; }
        public string WorkerName { get; }

        public WorkerTriggerNotification(string streamName, string workerName)
        {
            StreamName = streamName;
            WorkerName = workerName;
        }

        public override string ToString()
        {
            return $"{nameof(WorkerTriggerNotification)}<{StreamName}%{WorkerName}>";
        }

        public static WorkerTriggerNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(WorkerTriggerNotification))) throw new ArgumentException();

            var pattern = $@"{nameof(WorkerTriggerNotification)}<(?'{nameof(StreamName)}'.*)%(?'{nameof(WorkerName)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var streamName = match.Groups[nameof(StreamName)].Value;
            var workerName = match.Groups[nameof(WorkerName)].Value;

            return new WorkerTriggerNotification(streamName, workerName);
        }
    }
}