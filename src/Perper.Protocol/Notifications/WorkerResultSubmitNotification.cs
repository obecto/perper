using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class WorkerResultSubmitNotification
    {
        public string StreamName { get; }

        public WorkerResultSubmitNotification(string streamName)
        {
            StreamName = streamName;
        }

        public override string ToString()
        {
            return $"{nameof(WorkerResultSubmitNotification)}<{StreamName}>";
        }

        public static WorkerResultSubmitNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(WorkerResultSubmitNotification))) throw new ArgumentException();
            
            var pattern = $@"{nameof(WorkerResultSubmitNotification)}<(?'{nameof(StreamName)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var streamName = match.Groups[nameof(StreamName)].Value;
            
            return new WorkerResultSubmitNotification(streamName);
        }
    }
}