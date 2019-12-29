using System;
using System.Text.RegularExpressions;

namespace Perper.Protocol.Notifications
{
    public class WorkerTriggerNotification
    {
        public string StreamName { get; }

        public WorkerTriggerNotification(string streamName)
        {
            StreamName = streamName;
        }
        
        public override string ToString()
        {
            return $"{nameof(WorkerTriggerNotification)}<{StreamName}>";
        }

        public static WorkerTriggerNotification Parse(string stringValue)
        {
            if (!stringValue.StartsWith(nameof(WorkerTriggerNotification))) throw new ArgumentException();
            
            var pattern = $@"{nameof(WorkerTriggerNotification)}<(?'{nameof(StreamName)}'.*)>";
            var match = Regex.Match(stringValue, pattern);
            var streamName = match.Groups[nameof(StreamName)].Value;
            
            return new WorkerTriggerNotification(streamName);
        }
    }
}